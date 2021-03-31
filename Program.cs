using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static Globals;
using static Globals.DebugLevel;

class Program
{
    //=========================================================|
    //  Used For Logging                                       |     
    //=========================================================|
    private static readonly Type AREA = typeof(Program);

    public static async Task Main(string[] args)
    {
        readAllSettings();

        mirFleet.getFleetRobotIDs();

        initializeFleet();

        mirFleet.getInitialFleetData();

        logger(AREA, INFO, "==== Starting Main Loop ====");

        long i = 0;

        Stopwatch timer = new Stopwatch();
                  timer.Start();

        // Used to time how long it took Fleet to find a robot for a mission
        Stopwatch robotAssignmentTimer = new Stopwatch();
                  robotAssignmentTimer.Start();

        //====================================================|
        //  M A I N      L O O P                              |
        //====================================================|
        while (keepRunning)
        {
            logger(AREA, DEBUG, "==== Loop " + ++i + " Starting ====");
            logger(AREA, DEBUG, "Current Stopwatch Time: " + timer.Elapsed.TotalSeconds);

            if (SiemensPLC.plcConnected)
            {
                //====================================================|
                //  Poll the PLC for new tasks                        |
                //====================================================|
                SiemensPLC.poll();

                //====================================================|
                //  Perform tasks if there's a new message            |
                //  See Globals.Tasks for definitions                 |
                //====================================================|           
                if (SiemensPLC.newMsg)
                {
                    logger(AREA, INFO, "==== New Task From The PLC ====");

                    //====================================================|
                    //  Fleet Manager Tasks                               |
                    //====================================================|
                    if (SiemensPLC.newMsgs[0])
                    {
                        logger(AREA, INFO, "Task For Fleet");

                        SiemensPLC.updateTaskStatus(SiemensPLC.fleetID, Globals.TaskStatus.StartedProcessing);

                        int taskStatus = Globals.TaskStatus.StartedProcessing;

                        switch (SiemensPLC.fleetBlock.getTaskNumber())
                        {
                            case Tasks.GetScheduleStatus:
                                taskStatus = getScheduleStatus();
                                break;
                            case Tasks.SendMissionToScheduler:
                                robotAssignmentTimer.Restart();
                                taskStatus = sendMissionToScheduler(SiemensPLC.fleetBlock.getTaskParameter(), fleetID);
                                break;
                            case Tasks.CreateMission:
                                taskStatus = createMission();
                                break;
                            case Tasks.ClearScheduler:
                                taskStatus = clearScheduler();
                                break;
                            case Tasks.GetBattery:
                                taskStatus = getBattery();
                                break;
                            case Tasks.GetDistance:
                                taskStatus = getDistance();
                                break;
                            case Tasks.GetRobotStatus:
                                logger(AREA, ERROR, "Get Robot Status Task No Longer Suppported");
                                break;
                            default:
                                taskStatus = unknownMission(SiemensPLC.fleetID);
                                break;
                        }

                        //SiemensPLC.updateTaskStatus(fleetID, taskStatus);
                        //SiemensPLC.updateTaskStatus(fleetID, Globals.TaskStatus.StartedProcessing);

                        SiemensPLC.writeFleetBlock(Globals.TaskStatus.StartedProcessing);
                        //SiemensPLC.writeFleetBlock(taskStatus);

                        SiemensPLC.newMsgs[0] = false;

                        logger(AREA, DEBUG, "Finished Checking Fleet");
                    }

                    //====================================================|
                    //  Robot Tasks                                       |
                    //====================================================|
                    for (int j = 0; j < sizeOfFleet; j ++)
                    {
                        // Message 0 is reserved for Fleet tasks, so offset by 1
                        int robotMessage = j + 1;
                        
                        if (SiemensPLC.newMsgs[robotMessage])
                        {
                            logger(AREA, INFO, "Task For Robot: " + j);
                            logger(AREA, INFO, "Task Number: " + SiemensPLC.robots[j].getTaskNumber());
                            logger(AREA, INFO, "Task Parameter: " + SiemensPLC.robots[j].getTaskParameter());

                            //New Mission, so set Mission Status to Idle
                            mirFleet.robots[j].schedule.state_id = Globals.TaskStatus.Idle;

                            SiemensPLC.updateTaskStatus(j, Globals.TaskStatus.StartedProcessing);

                            int taskStatus = Globals.TaskStatus.StartedProcessing;

                            switch (SiemensPLC.robots[j].getTaskNumber())
                            {
                                case Tasks.GetScheduleStatus:
                                    taskStatus = getScheduleStatus();
                                    break;
                                case Tasks.SendMissionToScheduler:
                                    taskStatus = sendMissionToScheduler(SiemensPLC.robots[j].getTaskParameter(), j);
                                    break;
                                case Tasks.CreateMission:
                                    taskStatus = createMission();
                                    break;
                                case Tasks.ClearScheduler:
                                    taskStatus = clearScheduler();
                                    break;
                                case Tasks.GetBattery:
                                    taskStatus = getBattery(j);
                                    break;
                                case Tasks.GetDistance:
                                    taskStatus = getDistance(j);
                                    break;
                                case Tasks.GetRobotStatus:
                                    taskStatus = getRobotStatus(j);
                                    break;
                                case Tasks.SendRobotMission:
                                    taskStatus = sendMissionToScheduler(SiemensPLC.robots[j].getTaskParameter(), j);
                                    break;
                                case Tasks.ReleaseRobot:
                                    taskStatus = releaseRobot(j);
                                    break;
                                default:
                                    taskStatus = unknownMission(j);
                                    break;
                            }

                            SiemensPLC.updateTaskStatus(j, taskStatus);
                            //SiemensPLC.writeRobotBlock(j, taskStatus);

                            SiemensPLC.newMsgs[robotMessage] = false;
                        }
                    }

                    SiemensPLC.newMsg = false;

                    logger(AREA, INFO, "==== Completed Processing Tasks ====");
                }

                SiemensPLC.checkResponse();

                //====================================================|
                //  Read Alarms And Flag If Triggered                 |
                //====================================================|
                SiemensPLC.readAlarms();

                // Need to get registers prior to 
                mirFleet.getRegisters();

                //====================================================|
                //  Check Mission Progress From Fleet                 |
                //====================================================|
                checkMissionAssignment(robotAssignmentTimer);

                //====================================================|
                //  Fetch High Frequency Data for the PLC             |
                //====================================================|
                for (int k = 0; k < sizeOfFleet; k++)
                {
                    getRobotStatus(k);

                    getRobotStatusFromFleet(k);

                    SiemensPLC.writeRobotBlock(k);
                }

                // Check if we need to write to fleet at the end of every loop (might not be needed)
                getRobotGroups();
            }
            else
            {
                logger(AREA, ERROR, "AMR-Connect To PLC Comms Have Dropped");
            }

            SiemensPLC.checkConnectivity();

            // Poll MiR Fleet - async operation that happens every pollInterval
            if (timer.Elapsed.Seconds >= pollInterval)
            {
                logger(AREA, INFO, "Harvesting Robot Data And Checking Reports. " + timer.Elapsed.TotalSeconds + "s Since Last Poll");

                calculationsAndReporting();
                timer.Restart();
                mirFleet.pollRobots(); //TODO: -> make this quicker, get status from fleet?
            }

            checkConfigChanges();

            checkPLCReset();

            // For Debug Purposes
            SiemensPLC.printNewMessageStatus();

            logger(AREA, DEBUG, "==== Loop " + i + " Finished ====");

            Thread.Sleep(100); // Remove in live deployment
        }

        gracefulTermination();
    }

    /// <summary>
    /// 
    /// </summary>
    private static int getScheduleStatus()
    {
        logger(AREA, INFO, "==== Get Schedule Status ====");

        int restStatus = mirFleet.issueGetRequest("mission_scheduler/" + SiemensPLC.robotID, SiemensPLC.robotID);

        SiemensPLC.writeData("mission_schedule", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);

        logger(AREA, DEBUG, "==== Obtained Scheduler Status ====");

        return restStatus;
    }

    /// <summary>
    /// Sends a mission to Fleet Scheduler. If the robotID is the fleet ID, it sends a mision to fleet and checks which robot got assigned after a delay.
    /// </summary>
    /// <param name="plcMissionNumber">Mission Number from the PLC (Mirage & Fleet Mission No + 301)</param>
    /// <param name="robotID">Internal Robot ID</param>
    /// <returns></returns>
    private static int sendMissionToScheduler(int plcMissionNumber, int robotID)
    {
        // PLC Missions start at 301. Internal Mirage and Fleet missions start at 0.
        int mission_number = plcMissionNumber - PLCMissionOffset;
        int restStatus;
        int waitTimeForMissionAssignment = 10;

        logger(AREA, INFO, "Sending A New Mission To The Fleet Scheduler");
        logger(AREA, INFO, "Mission " + plcMissionNumber + " : " + mirFleet.fleetManager.Missions[mission_number].name);
        mirFleet.fleetManager.Missions[mission_number].print();

        if (robotID == fleetID)
        {
            logger(AREA, INFO, "It's For Any Robot In The Available Group");

            if(mission_number > 16 && mission_number < 25)
            {
                mission_number = mission_number + 45;
            }

            // If successfull, this returns Schedule ID and stores it in mirFleet.fleetManager.schedule.id
            restStatus = mirFleet.fleetManager.sendScheduleRequest(mirFleet.fleetManager.Missions[mission_number].postRequest());

            // Wait for a response from Fleet, to see which robot it assigns
            // Might not be needed - TODO: check if necessary
            Thread.Sleep(waitTimeForMissionAssignment);

            // Check the mission_scheduler we've just created to return with the robot ID
            // If we're still waiting for a response (which we will)
            if (mirFleet.fleetManager.schedule.working_response)
            {
                int currentMissionRobot = 0;

                logger(AREA, DEBUG, "Mission Scheduler Fleet Robot ID is: " + mirFleet.fleetManager.schedule.robot_id);
                logger(AREA, DEBUG, "Mission Schedule ID is: " + mirFleet.fleetManager.schedule.id);

                // Check fleet manager mission scheduler to see which robot is doing the mission
                restStatus = mirFleet.issueGetRequest("mission_scheduler/" + mirFleet.fleetManager.schedule.id, fleetID);

                mirFleet.fleetManager.schedule.mission_number = mission_number;

                logger(AREA, DEBUG, "Mission Scheduler Robot ID After polling is: " + mirFleet.fleetManager.schedule.robot_id);

                if (restStatus == Globals.TaskStatus.CompletedNoErrors && mirFleet.fleetManager.schedule.robot_id != 0)
                {
                    currentMissionRobot = mirFleet.getInternalRobotID(mirFleet.fleetManager.schedule.robot_id);

                    //mirFleet.returnParameter = (short)(currentMissionRobot+1);
                    mirFleet.returnParameter = (short)(mirFleet.robots[currentMissionRobot].plcRobotID);

                    mirFleet.robots[currentMissionRobot].schedule.id = mirFleet.fleetManager.schedule.id;

                    logger(AREA, DEBUG, "Mission Assigned Immediately After Despatch, To Robot: " + currentMissionRobot);

                    occupyRobot(currentMissionRobot);

                    // Add a record to the robot's job ledger - we're starting a new job since this is coming from Fleet
                    mirFleet.robots[currentMissionRobot].currentJob.startJob(mission_number, mirFleet.fleetManager.Missions[mission_number].name);

                    mirFleet.fleetManager.schedule.working_response = false;
                }
                else
                {
                    restStatus = Globals.TaskStatus.StartedProcessing;
                    mirFleet.fleetManager.schedule.working_response = true;
                }
            }
        }
        else
        {
            int fleetRobotID = mirFleet.robots[robotID].fleetRobotID;

            if (mission_number < 9)
            {
                mission_number = mission_number + 53;
            }

            logger(AREA, INFO, "It's Sent To Internal Robot ID: " + robotID + " Fleet Robot ID: " + fleetRobotID);

            restStatus = mirFleet.robots[robotID].sendScheduleRequest(mirFleet.fleetManager.Missions[mission_number].postRequest(fleetRobotID));

            logger(AREA, INFO, "The Schedule ID IS: " + mirFleet.robots[robotID].schedule.id);

            // Add a record to the robot's job ledger - this is a new mission added on top of the existing mission stack
            if(plcMissionNumber != Tasks.ReleaseRobot)
            { 
                mirFleet.robots[robotID].currentJob.addMission(mission_number, mirFleet.fleetManager.Missions[mission_number].name);
            }

            // No need to send to busy group as all robots which get direct commands are already in it
        }

        logger(AREA, DEBUG, "Mission Sent To Scheduler");
        logger(AREA, DEBUG, "Status: " + restStatus);

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int sendFireAlarm()
    {
        logger(AREA, INFO, "==== Send Fire Alarm To Scheduler ====");

        // Need to add whether to turn alarm on/off and which alarm to affect from PLC
        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.FireAlarm.putRequest(true, 1));

        //SiemensPLC.updateTaskStatus(restStatus);
        //logger(AREA, DEBUG, "Status: " + restStatus);
        logger(AREA, DEBUG, "==== Fire Alarm Sent ====");

        //Program.alarm_on = !Program.alarm_on;

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int sendRobotGroup(int robotID, int robotGroupID)
    {
        logger(AREA, INFO, "==== Send (Change) Robot Group To Scheduler ====");

        //int robotID = 1;
        //int robotGroupID = 2;

        // Need to add whether to turn alarm on/off and which alarm to affect from PLC
        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Group.putRequest(robotID, robotGroupID));

        //SiemensPLC.updateTaskStatus(restStatus);
        logger(AREA, DEBUG, "Status: " + restStatus);
        logger(AREA, DEBUG, "==== Robot Group Changed ====");

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private static void getRobotGroups()
    {
        logger(AREA, DEBUG, "Saving Robot Groups In Fleet PLC Block");

        // Comment out for now - we're relying on keeping track of the groups ourselves
        //int restStatus = mirFleet.issueGetRequest("robots?whitelist=robot_group_id", SiemensPLC.fleetID);

        fleetMemoryToPLC();
        SiemensPLC.writeFleetBlock(SiemensPLC.fleetBlock.getTaskStatus());
    }

    /// <summary>
    /// 
    /// </summary>
    private static int createMission()
    {
        logger(AREA, INFO, "==== Create New Mission In Robot " + SiemensPLC.robotID + " ====");

        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[0].createMission(SiemensPLC.parameter));

        SiemensPLC.updateTaskStatus(restStatus);

        logger(AREA, DEBUG, "==== Created New Mission ====");

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int clearScheduler()
    {
        logger(AREA, INFO, "==== Clear Mission Schedule ====");

        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[0].deleteRequest());

        SiemensPLC.updateTaskStatus(SiemensPLC.fleetID, restStatus);

        logger(AREA, DEBUG, "==== Cleared Mission Scheduler ====");

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int getBattery()
    {
        Console.WriteLine("==== Get Battery Life ====");

        int restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

        //SiemensPLC.writeData("battery", restStatus, mirFleet.robots[SiemensPLC.robotID].s.battery_percentage);

        return restStatus;
    }

    private static int getBattery(int robotID)
    {
        Console.WriteLine("==== Get Battery Life ====");

        int restStatus = mirFleet.issueGetRequest("status", robotID);

        //SiemensPLC.writeData("battery", restStatus, mirFleet.robots[SiemensPLC.robotID].s.battery_percentage);

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int getDistance(int robotID)
    {
        logger(AREA, INFO, "==== Get Distance Travelled ====");

        int restStatus = mirFleet.issueGetRequest("status", robotID);

        logger(AREA, DEBUG, "==== Distance Travelled Status Fetched And Saved ====");

        return restStatus;
    }

    /// <summary>
    /// Used to check the mission status on both the Robot and the Fleet level
    /// </summary>
    private static void checkMissionAssignment(Stopwatch robotAssignment)
    {
        logger(AREA, DEBUG, "Checking Mission Assignment");

        if (mirFleet.fleetManager.schedule.working_response)
        {
            // We're waiting for the Fleet to assign a robot to a mission in queue

            int currentMissionRobot = 0;
            int restStatus = 0;

            logger(AREA, INFO, "Current Scheduler Fleet Robot ID is: " + mirFleet.fleetManager.schedule.robot_id);
            logger(AREA, INFO, "Current Mission Schedule ID is: " + mirFleet.fleetManager.schedule.id);

            restStatus = mirFleet.issueGetRequest("mission_scheduler/" + mirFleet.fleetManager.schedule.id, fleetID);

            logger(AREA, DEBUG, "Mission Scheduler Robot ID After polling is: " + mirFleet.fleetManager.schedule.robot_id);

            if (restStatus == Globals.TaskStatus.CompletedNoErrors && mirFleet.fleetManager.schedule.robot_id != 0)
            {
                logger(AREA, INFO, "It Took Fleet " + robotAssignment.Elapsed.TotalSeconds + " Seconds To Find An Available Robot");
                robotAssignment.Stop();

                //currentMissionRobot = mirFleet.fleetManager.schedule.robot_id - 3; // TODO: This is as the robots are offset by one in fleet - change to be better

                currentMissionRobot = mirFleet.getInternalRobotID(mirFleet.fleetManager.schedule.robot_id);

                mirFleet.returnParameter = (short)(mirFleet.robots[currentMissionRobot].plcRobotID);

                logger(AREA, INFO, "Mission " + mirFleet.fleetManager.schedule.id + " Has A New Robot (Mirage: " + currentMissionRobot + ", Fleet: " + mirFleet.fleetManager.schedule.robot_id + ")");

                mirFleet.robots[currentMissionRobot].schedule.id = mirFleet.fleetManager.schedule.id;

                occupyRobot(currentMissionRobot);

                restStatus = Globals.TaskStatus.CompletedNoErrors;

                // Add a record to the robot's job ledger - this is a new mission added on top of the existing mission stack
                int mission_number = mirFleet.fleetManager.schedule.mission_number;
                mirFleet.robots[currentMissionRobot].currentJob.startJob(mission_number, mirFleet.fleetManager.Missions[mission_number].name);

                mirFleet.fleetManager.schedule.working_response = false;
            }
            else
            {
                restStatus = Globals.TaskStatus.StartedProcessing;
                mirFleet.fleetManager.schedule.working_response = true;
            }

            SiemensPLC.writeFleetBlock(restStatus);
        }

        // Check the mission status for each robot
        for (int r = 0; r < sizeOfFleet; r++)
        {
            int restStatus = 0;

            // Don't check if we're idling or if we aborted the job
            // Used to be: if (mirFleet.robots[r].schedule.id != Globals.TaskStatus.Idle)
            // Now we don't scan if the PLC Task Status is idle (since the mir isn't doing anything)
            // && SiemensPLC.robots[r].getPLCTaskStatus() != Globals.TaskStatus.Idle
            if (mirFleet.robots[r].schedule.id != Globals.TaskStatus.Idle && 
                (SiemensPLC.robots[r].getPLCTaskStatus() != Globals.TaskStatus.Idle 
                || SiemensPLC.fleetBlock.getPLCTaskStatus() != Globals.TaskStatus.Idle 
                || mirFleet.robots[r].schedule.state_id != Globals.TaskStatus.Idle))
            {
                // Check the status of the mission assigned to robot r
                logger(AREA, INFO, "==== Robot ID: " + r + " And Mission Scheduler Robot ID is: " + mirFleet.robots[r].fleetRobotID + " ====");
                logger(AREA, INFO, "Mission Schedule ID is: " + mirFleet.robots[r].schedule.id);

                restStatus = mirFleet.checkMissionSchedule("mission_scheduler/" + mirFleet.robots[r].schedule.id + "?whilelist=state", SiemensPLC.fleetID, r);

                logger(AREA, INFO, "Mission Status For Robot: " + r + " is: " + mirFleet.robots[r].schedule.state);

                if(SiemensPLC.robots[r].getTaskParameter() == 351 || SiemensPLC.robots[r].getTaskParameter() == 352)
                {
                    logger(AREA, INFO, "Conveyor Running On MiR is: " + mirFleet.robots[r].Registers[1].value);
                    logger(AREA, INFO, "Mission Status is: " + mirFleet.robots[r].schedule.state_id);

                    if (mirFleet.robots[r].schedule.state == "Pending")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.TaskReceivedFromPLC;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Executing" && (int)(mirFleet.robots[r].Registers[1].value) == 1)
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Outbound")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.TaskReceivedFromPLC;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Aborted")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CouldntProcessRequest;
                        mirFleet.robots[r].schedule.id = 0;
                        SiemensPLC.updateTaskStatus(r, Globals.TaskStatus.CouldntProcessRequest);
                        //mirFleet.robots[r].currentJob.finishMission();
                        mirFleet.robots[r].currentJob.finishJob(r, true);
                    }
                    else if (mirFleet.robots[r].schedule.state == "Done")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CompletedNoErrors;
                        mirFleet.robots[r].schedule.id = 0;
                        mirFleet.robots[r].currentJob.finishMission();
                    }
                }
                else
                {
                    if (mirFleet.robots[r].schedule.state == "Pending")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.TaskReceivedFromPLC;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Executing")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Outbound")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Aborted")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CouldntProcessRequest;
                        SiemensPLC.updateTaskStatus(r, Globals.TaskStatus.CouldntProcessRequest);
                        //mirFleet.robots[r].currentJob.finishMission();
                        mirFleet.robots[r].currentJob.finishJob(r, true);
                        mirFleet.robots[r].schedule.id = 0;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Done")
                    {
                        // Used to be else if (mirFleet.robots[r].schedule.state == "Done")

                        if ((int)(mirFleet.robots[r].Registers[2].value) == 1)
                        {
                            mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CompletedNoErrors;
                            mirFleet.robots[r].schedule.id = 0;
                            mirFleet.robots[r].currentJob.finishMission();
                        }
                        else if(SiemensPLC.robots[r].getTaskParameter() == 353)
                        {
                            mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CompletedNoErrors;
                            mirFleet.robots[r].schedule.id = 0;
                            mirFleet.robots[r].currentJob.finishMission();
                        }
                        else
                        {
                            mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CouldntProcessRequest;
                            mirFleet.robots[r].schedule.id = 0;
                            mirFleet.robots[r].currentJob.finishMission();
                        }
                    }
                    else
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }
                }

                logger(AREA, DEBUG, "Mission Status For Robot: " + r + " State ID is: " + mirFleet.robots[r].schedule.state_id);
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    private static int getDistance()
    {
        logger(AREA, INFO, "==== Get Distance Travelled ====");

        int restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

        SiemensPLC.writeData("moved", restStatus, mirFleet.robots[SiemensPLC.robotID].s.moved);

        logger(AREA, DEBUG, "==== Distance Travelled Status Fetched And Saved ====");

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int getRobotStatus(int robotID)
    {
        logger(AREA, DEBUG, "Get Robot Status From Robots");

        int restStatus = mirFleet.issueGetRequest("status", robotID);

        //SiemensPLC.writeRobotBlock(robotID);

        logger(AREA, DEBUG, "==== Robot Status Fetched And Saved ====");

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="robotID"></param>
    /// <returns></returns>
    private static int getRobotStatusFromFleet(int robotID)
    {
        logger(AREA, DEBUG, "Get Robot Status From Fleet");

        int restStatus = mirFleet.issueGetRequest("robotStatusFromFleet", robotID);

        //SiemensPLC.writeRobotBlock(robotID);

        logger(AREA, DEBUG, "==== Robot Status Fetched And Saved ====");

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int unknownMission(int robotID)
    {
        logger(AREA, ERROR, "==== Unknown Mission ====");

        SiemensPLC.updateTaskStatus(robotID, Globals.TaskStatus.CouldntProcessRequest);

        return Globals.TaskStatus.CouldntProcessRequest;

        // Issue an alert
    }

    /// <summary>
    /// First, delete the robot from any charging groups it was assigned to prior to the call.
    /// Then, add the robot to the charging group we want
    /// </summary>
    /// <param name="robotID">Internal Mirage Robot ID</param>
    private static int releaseRobot(int robotID)
    {
        int restStatus;
        int taskStat;
        int fleetRobotID = mirFleet.robots[robotID].fleetRobotID;

        logger(AREA, INFO, "Releasing Robot " + robotID + " (Fleet ID: " + fleetRobotID + ") From The Busy Group");

        // Sending a delete request
        restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.busy.deleteRequest(fleetRobotID));

        if(restStatus == Globals.TaskStatus.CompletedNoErrors)
        {
            logger(AREA, INFO, "Robot " + fleetRobotID + " Released From Empty Charge");

            // We succeeded at deleting the robot from the old charging group
            // Wait for a bit and assign the empty/available charging group
            Thread.Sleep(50);
            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.available.postRequest(fleetRobotID));

            if(restStatus == Globals.TaskStatus.CompletedNoErrors)
            {
                taskStat = Globals.TaskStatus.CompletedNoErrors;
            }
            else
            {
                taskStat = Globals.TaskStatus.FatalError;
                logger(AREA, ERROR, "Failed To Assign To Full Charge Group");
            }
        }
        else
        {
            logger(AREA, WARNING, "Failed To Remove From Empty Charge - Potentially It Was Never Assigned To The Group");

            // We failed to delete the robot from an old charging group
            // If we failed because the robot wasn't in the group, just put it in the charging group
            Thread.Sleep(50);
            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.available.postRequest(fleetRobotID));

            if (restStatus == Globals.TaskStatus.CompletedNoErrors)
            {
                taskStat = Globals.TaskStatus.CompletedNoErrors;
            }
            else
            {
                taskStat = Globals.TaskStatus.FatalError;
                logger(AREA, ERROR, "Failed To Assign To Full Charge Group");
            }
        }

        //==================================================================|
        // Need to send a dummy mission so as to prompt the robot           |
        // The robot will enter an "Executing State" and at the end         |
        // it will go back to the idle state.                               |
        //==================================================================|
        sendMissionToScheduler(Tasks.ReleaseRobot, robotID);

        //==================================================================|
        // Put the robot into the available group. This enables it to       |
        // go charge and also start new jobs - otherwise it's going         |
        // to stick to its current tasks                                    |
        // Available Group ID : 3                                           |
        // TODO: Add a better way to handle this - response from fleet?     |
        //==================================================================|
        try
        {
            sendRobotGroup(fleetRobotID, 3);
        }
        catch(Exception e)
        {
            logger(AREA, ERROR, "Failed To Put Robot Into Available Group");
            logger(AREA, ERROR, "Exception: ", e);
        }

        // Complete the job, save to DB and clear the job ledger
        mirFleet.robots[robotID].currentJob.finishJob(robotID, false);

        // Add one to available group, subtract one from busy group
        //mirFleet.groups[1]++;
        //mirFleet.groups[2]--;

        return taskStat;
    }

    /// <summary>
    /// First, delete the robot from any charging groups it was assigned to prior to the call.
    /// Then, add the robot to the busy charging group
    /// </summary>
    /// <param name="robotID">The Internal Mirage Robot ID</param>
    private static int occupyRobot(int robotID)
    {
        int restStatus;
        int taskStat;
        int fleetRobotID = mirFleet.robots[robotID].fleetRobotID;

        logger(AREA, INFO, "Releasing Robot " + fleetRobotID + " (Fleet ID: " + fleetRobotID + ") From Busy Charging Group");

        // Sending a delete request
        restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.available.deleteRequest(fleetRobotID));

        if (restStatus == Globals.TaskStatus.CompletedNoErrors)
        {
            logger(AREA, INFO, "Robot " + fleetRobotID + " Put Into Empty Charge Group");

            // We succeeded at deleting the robot from the old charging group
            // Wait for a bit and assign the empty/available charging group
            Thread.Sleep(50);
            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.busy.postRequest(fleetRobotID));

            if (restStatus == Globals.TaskStatus.CompletedNoErrors)
            {
                // We managed to assign it a new group
                // Update the task status so PLC knows we're done 
                taskStat = restStatus;
            }
            else
            {
                taskStat = Globals.TaskStatus.FatalError;
            }
        }
        else
        {
            // We failed to delete the robot from an old charging group
            // If we failed because the robot wasn't in the group, just put it in the charging group

            Thread.Sleep(50);
            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.busy.postRequest(fleetRobotID));

            if (restStatus == Globals.TaskStatus.CompletedNoErrors)
            {
                // We managed to assign it a new group
                // Update the task status so PLC knows we're done 
                taskStat = restStatus;
            }
            else
            {
                taskStat = Globals.TaskStatus.FatalError;
            }
        }

        // Put the robot into the busy group
        try
        {
            // Available group has group id 3
            sendRobotGroup(fleetRobotID, 4);
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "Failed To Put Robot Into Available Group");
            logger(AREA, ERROR, "Exception: ", e);
        }

        // Add one to busy group, subtract one from available group
        //mirFleet.groups[2]++;
        //mirFleet.groups[1]--;

        return taskStat;
    }

    /// <summary>
    /// 
    /// </summary>
    private static void calculationsAndReporting()
    {
        reports.reportingPass();
    }
}

