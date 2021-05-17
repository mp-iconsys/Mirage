using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using static SiemensPLC;
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

        initializeFleet();

        logger(AREA, INFO, "==== Starting Main Loop ====");

        long i = 0;

        // Used for saving data into DB
        Stopwatch timer = new Stopwatch();
                  timer.Start();

        // Used to time how long it took Fleet to find a robot for a mission
        Stopwatch robotAssignmentTimer = new Stopwatch();
                  robotAssignmentTimer.Start();

        // Timer to check robot downtime
        Stopwatch robotDowntimeTimer = new Stopwatch();

        //====================================================|
        //  M A I N      L O O P                              |
        //====================================================|
        while (keepRunning)
        {
            logger(AREA, DEBUG, "==== Loop " + ++i + " Starting ====");

            if (plcConnected)
            {
                //====================================================|
                //  Poll the PLC for new tasks                        |
                //====================================================|
                poll();

                //====================================================|
                //  Perform tasks if there's a new message            |
                //  See Globals.Tasks for definitions                 |
                //====================================================|           
                if (newMsg)
                {
                    logger(AREA, INFO, "==== New Task(s) From The PLC ====");

                    //====================================================|
                    //  Fleet Manager Tasks                               |
                    //====================================================|
                    if (newMsgs[0])
                    {
                        logger(AREA, INFO, "Task For Fleet");
                        logger(AREA, INFO, "Task Number: " + fleetBlock.getTaskNumber());
                        logger(AREA, INFO, "Task Parameter: " + fleetBlock.getTaskParameter());

                        updateTaskStatus(fleetID, Globals.TaskStatus.StartedProcessing);

                        int restStatus = Globals.TaskStatus.StartedProcessing;

                        switch (fleetBlock.getTaskNumber())
                        {
                            case Tasks.GetScheduleStatus:
                                checkMissionAssignment(robotAssignmentTimer);
                                break;
                            case Tasks.SendMissionToScheduler:
                                robotAssignmentTimer.Restart();
                                restStatus = sendMissionToScheduler(fleetBlock.getTaskParameter(), fleetID);
                                break;
                            case Tasks.CreateMission:
                                restStatus = createMission();
                                break;
                            case Tasks.ClearScheduler:
                                restStatus = clearScheduler();
                                break;  
                            default:
                                restStatus = unknownMission(fleetID);
                                break;
                        }

                        writeFleetBlock(Globals.TaskStatus.StartedProcessing);

                        newMsgs[0] = false;

                        logger(AREA, DEBUG, "Finished Checking Fleet");
                    }

                    //====================================================|
                    //  Robot Tasks                                       |
                    //====================================================|
                    for (int robotID = 0; robotID < sizeOfFleet; robotID++)
                    {
                        // Message 0 is reserved for Fleet tasks, so offset by 1
                        int robotMessage = robotID + 1;
                        
                        if (newMsgs[robotMessage])
                        {
                            logger(AREA, INFO, "Task For " + mirFleet.robots[robotID].s.robot_name);
                            logger(AREA, INFO, "Task Number: " + robots[robotID].getTaskNumber());
                            logger(AREA, INFO, "Task Parameter: " + robots[robotID].getTaskParameter());

                            //New Mission, so set Mission Status to Idle
                            mirFleet.robots[robotID].schedule.state_id = Globals.TaskStatus.Idle;

                            updateTaskStatus(robotID, Globals.TaskStatus.StartedProcessing);

                            int taskStatus = Globals.TaskStatus.StartedProcessing;

                            switch (robots[robotID].getTaskNumber())
                            {
                                case Tasks.SendMissionToScheduler:
                                    taskStatus = sendMissionToScheduler(robots[robotID].getTaskParameter(), robotID);
                                    break;
                                case Tasks.CreateMission:
                                    taskStatus = createMission();
                                    break;
                                case Tasks.ClearScheduler:
                                    taskStatus = clearScheduler();
                                    break;
                                case Tasks.SendRobotMission:
                                    taskStatus = sendMissionToScheduler(robots[robotID].getTaskParameter(), robotID);
                                    break;
                                case Tasks.ReleaseRobot:
                                    taskStatus = releaseRobot(robotID);
                                    break;
                                default:
                                    taskStatus = unknownMission(robotID);
                                    break;
                            }

                            updateTaskStatus(robotID, taskStatus);

                            newMsgs[robotMessage] = false;
                        }
                    }

                    newMsg = false;

                    logger(AREA, INFO, "==== Completed Processing Task(s) ====");
                }

                checkPLCResponse();

                //====================================================|
                //  Read Alarms And Flag If Triggered                 |
                //====================================================|
                readAlarms();

                checkConveyors();

                //====================================================|
                // Fetch registers prior to checking mission status   |
                // This is as we need current register values for     |
                // Conveying on and off                               |
                //====================================================|
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
                    if(wifiScanEnabled)
                    {
                        // Take WiFi Network scans here
                    }

                    getRobotStatusFromFleet(k);

                    writeRobotBlock(k);
                }

                //====================================================|
                // TODO: Check if we need to write to fleet at        |
                // the end of every loop (might not be needed)        |
                //====================================================|
                getRobotGroups();
            }
            else
            {
                logger(AREA, ERROR, "AMR-Connect To PLC Comms Have Dropped");
            }

            checkConnectivity();

            // Poll MiR Fleet - async operation that happens every pollInterval
            if (timer.Elapsed.Seconds >= pollInterval)
            {
                logger(AREA, INFO, "Harvesting Data " + timer.Elapsed.TotalSeconds + "s Since Last Poll");

                checkRESTConnectivity(robotDowntimeTimer);

                //calculationsAndReporting();
                timer.Restart();
                mirFleet.pollRobots();
            }

            if(robotDowntimeTimer.Elapsed.TotalMilliseconds > 1000)
            {
                for(int robotID = 0; robotID < sizeOfFleet; robotID++)
                {
                    if(!mirFleet.robots[robotID].isLive)
                    {
                        mirFleet.robots[robotID].killConnectionCheckRequest(robotDowntimeTimer);
                    }
                }
            }

            checkConfigChanges();

            checkPLCReset();

            //====================================================|
            // For Debug Purposes                                 |
            //====================================================|
            printNewMessageStatus();

            logger(AREA, DEBUG, "==== Loop " + i + " Finished ====");
        }

        gracefulTermination();
    }

    /// <summary>
    /// 
    /// </summary>
    private static void clearDB()
    {

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timer"></param>
    private static void checkRESTConnectivity(Stopwatch timer)
    {
        logger(AREA, DEBUG, "Checking REST Connections");

        if(!mirFleet.fleetManager.isLive)
        {
            mirFleet.fleetManager.CheckConnection(timer);
        }

        for(int robotID = 0; robotID < sizeOfFleet; robotID++)
        {
            if(!mirFleet.robots[robotID].isLive)
            {
                logger(AREA, INFO, "Checking " + mirFleet.robots[robotID].s.robot_name + " Connection");

                mirFleet.robots[robotID].CheckConnection(timer);
            }
        }
    }

    /// <summary>
    /// Sends a mission to Fleet Scheduler. If the robotID is the fleet ID, it sends a mision to fleet and checks which robot got assigned after a delay.
    /// </summary>
    /// <param name="plcMissionNumber">Mission Number from the PLC (Mirage & Fleet Mission No + 301)</param>
    /// <param name="robotID">Internal Robot ID</param>
    /// <returns></returns>
    private static int sendMissionToScheduler(int plcMissionNumber, int robotID)
    {
        //======================================================|
        // PLC Missions start at 301.                           |
        // Internal Mirage and Fleet missions start at 0.       |
        //======================================================|
        int mission_number = plcMissionNumber - PLCMissionOffset;
        int restStatus;

        logger(AREA, INFO, "==== Sending A New Mission To The Scheduler ====");

        if (robotID == fleetID)
        {
            //======================================================|
            // Empty Tote missions sent to Fleet need to be in the  |
            // Available mission group. Offset for these is 45.     |
            //======================================================|
            if (mission_number > 16 && mission_number < 25)
            {
                mission_number = mission_number + 45;
            }

            logger(AREA, INFO, "Mission For Fleet: Any Robot In The Available Group");
            logger(AREA, INFO, "PLC Mission No:" + plcMissionNumber + " : " + mirFleet.fleetManager.Missions[mission_number].name);

            //======================================================|
            // If successfull, this returns Schedule ID and         |
            // stores it in mirFleet.fleetManager.schedule.id       |
            // This is what we then poll when we check mission      |
            // assignment to see what robot was assigned            |
            //======================================================|
            restStatus = mirFleet.fleetManager.sendScheduleRequest(mirFleet.fleetManager.Missions[mission_number].postRequest());

            mirFleet.fleetManager.schedule.mission_number = mission_number;
            mirFleet.fleetManager.schedule.plc_mission_number = plcMissionNumber;
        }
        else
        {
            int fleetRobotID = mirFleet.robots[robotID].fleetRobotID;

            if (mission_number < 9)
            {
                mission_number = mission_number + 53;
            }

            logger(AREA, INFO, "Mission Sent To " + mirFleet.robots[robotID].s.robot_name);
            logger(AREA, INFO, "PLC Mission No:" + plcMissionNumber + " : " + mirFleet.fleetManager.Missions[mission_number].name);

            if(mission_number == (Tasks.ReleaseRobot - PLCMissionOffset))
            {
                restStatus = mirFleet.robots[robotID].sendScheduleRequest(mirFleet.fleetManager.Missions[mission_number].urgentReleaseRobot(fleetRobotID));
            }
            else
            {
                restStatus = mirFleet.robots[robotID].sendScheduleRequest(mirFleet.fleetManager.Missions[mission_number].postRequest(fleetRobotID));
            }

            mirFleet.robots[robotID].schedule.mission_number = mission_number;
            mirFleet.robots[robotID].schedule.plc_mission_number = plcMissionNumber;

            logger(AREA, INFO, "The Schedule ID IS: " + mirFleet.robots[robotID].schedule.id);

            // Add a record to the robot's job ledger - this is a new mission added on top of the existing mission stack
            if(plcMissionNumber != Tasks.ReleaseRobot)
            { 
                mirFleet.robots[robotID].currentJob.addMission(mission_number, mirFleet.fleetManager.Missions[mission_number].name);
            }
        }

        logger(AREA, INFO, "==== Mission Sent To Scheduler ====");
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
        string group_name = "";

        if(robotGroupID == mirFleet.fleet_available_group)
        {
            group_name = "Available Group (ID: " + robotGroupID + ")";
        }
        else if(robotGroupID == mirFleet.fleet_busy_group)
        {
            group_name = "Busy Group (ID: " + robotGroupID + ")";
        }
        else if(robotGroupID == mirFleet.fleet_offline_group)
        {
            group_name = "Offline Group (ID: " + robotGroupID + ")";
        }
        else
        {
            group_name = "Unknown Group With ID: " + robotGroupID;
        }

        logger(AREA, INFO, mirFleet.robots[robotID].s.robot_name + " Is Changing Robot Group To -> " + group_name);

        int fleetRobotID = mirFleet.robots[robotID].fleetRobotID;
        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Group.putRequest(fleetRobotID, robotGroupID));

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

        fleetMemoryToPLC();
        SiemensPLC.writeFleetBlock(SiemensPLC.fleetBlock.getTaskStatus());
    }

    /// <summary>
    /// 
    /// </summary>
    private static int createMission()
    {
/*        logger(AREA, INFO, "==== Create New Mission In Robot " + SiemensPLC.robotID + " ====");

        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[0].createMission(SiemensPLC.parameter));*/

        logger(AREA, DEBUG, "==== Created New Mission ====");

        return 0;

        //return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int clearScheduler()
    {
        logger(AREA, INFO, "==== Clear Mission Schedule ====");

        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[0].deleteRequest());

        SiemensPLC.updateTaskStatus(fleetID, restStatus);

        logger(AREA, DEBUG, "==== Cleared Mission Scheduler ====");

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

            if(mirFleet.fleetManager.schedule.print_working_response)
            { 
                logger(AREA, INFO, "Waiting To Assign Robot For Mission " + mirFleet.fleetManager.schedule.plc_mission_number + " : " + mirFleet.fleetManager.Missions[mirFleet.fleetManager.schedule.mission_number].name);
                mirFleet.fleetManager.schedule.print_working_response = false;
            }
            //logger(AREA, INFO, "Current Scheduler Fleet Robot ID is: " + mirFleet.fleetManager.schedule.robot_id);
            //logger(AREA, INFO, "Current Mission Schedule ID is: " + mirFleet.fleetManager.schedule.id);

            restStatus = mirFleet.issueGetRequest("mission_scheduler/" + mirFleet.fleetManager.schedule.id, fleetID);

            //logger(AREA, DEBUG, "Mission Scheduler Robot ID After polling is: " + mirFleet.fleetManager.schedule.robot_id);

            if (restStatus == Globals.TaskStatus.CompletedNoErrors && mirFleet.fleetManager.schedule.robot_id != 0)
            {
                robotAssignment.Stop();

                currentMissionRobot = mirFleet.getInternalRobotID(mirFleet.fleetManager.schedule.robot_id);

                mirFleet.returnParameter = (short)(mirFleet.robots[currentMissionRobot].plcRobotID);

                //logger(AREA, INFO, "Mission " + mirFleet.fleetManager.schedule.id + " Has A New Robot (Mirage: " + currentMissionRobot + ", Fleet: " + mirFleet.fleetManager.schedule.robot_id + ")");
                logger(AREA, INFO, "Mission " + mirFleet.fleetManager.schedule.plc_mission_number + ": " + mirFleet.fleetManager.Missions[mirFleet.fleetManager.schedule.mission_number].name + " Was Assigned To " + mirFleet.robots[currentMissionRobot].s.robot_name);
                logger(AREA, INFO, "It Took Fleet " + robotAssignment.Elapsed.TotalSeconds + " Seconds To Assign A Robot");

                mirFleet.robots[currentMissionRobot].schedule.id = mirFleet.fleetManager.schedule.id;

                occupyRobot(currentMissionRobot);

                restStatus = Globals.TaskStatus.CompletedNoErrors;

                // Add a record to the robot's job ledger - this is a new mission added on top of the existing mission stack
                int mission_number = mirFleet.fleetManager.schedule.mission_number;
                mirFleet.robots[currentMissionRobot].currentJob.startJob(mission_number, mirFleet.fleetManager.Missions[mission_number].name);

                mirFleet.fleetManager.schedule.working_response = false;
                mirFleet.fleetManager.schedule.print_working_response = true;
            }
            else
            {
                restStatus = Globals.TaskStatus.StartedProcessing;
                mirFleet.fleetManager.schedule.working_response = true;
                mirFleet.fleetManager.schedule.print_working_response = false;
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

            // Also changed:   
            // if (mirFleet.robots[r].schedule.id != Globals.TaskStatus.Idle && 
            // To:
            // if (mirFleet.robots[r].schedule.id != 0 && 
            if (mirFleet.robots[r].schedule.id != 0 && 
                (robots[r].getPLCTaskStatus() != Globals.TaskStatus.Idle 
                || fleetBlock.getPLCTaskStatus() != Globals.TaskStatus.Idle 
                || mirFleet.robots[r].schedule.state_id != Globals.TaskStatus.Idle))
            {
                
                // Check the status of the mission assigned to robot r
                //logger(AREA, INFO, "==== Robot ID: " + r + " And Mission Scheduler Robot ID is: " + mirFleet.robots[r].fleetRobotID + " ====");
                //logger(AREA, INFO, "Mission Schedule ID is: " + mirFleet.robots[r].schedule.id);

                restStatus = mirFleet.checkMissionSchedule("mission_scheduler/" + mirFleet.robots[r].schedule.id + "?whilelist=state", fleetID, r);

                if(mirFleet.robots[r].schedule.state != mirFleet.robots[r].schedule.old_state)
                {
                    logger(AREA, INFO, mirFleet.robots[r].s.robot_name + " Is In State: " + mirFleet.robots[r].schedule.old_state + " -> " + mirFleet.robots[r].schedule.state + " For Mission " + mirFleet.robots[r].schedule.plc_mission_number + " : " + mirFleet.fleetManager.Missions[mirFleet.robots[r].schedule.mission_number].name);
                    mirFleet.robots[r].schedule.old_state = mirFleet.robots[r].schedule.state;
                }

                if (robots[r].getTaskParameter() == 351 || robots[r].getTaskParameter() == 352)
                {
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
                        updateTaskStatus(r, Globals.TaskStatus.CouldntProcessRequest);
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
                        updateTaskStatus(r, Globals.TaskStatus.CouldntProcessRequest);
                        //mirFleet.robots[r].currentJob.finishMission();
                        mirFleet.robots[r].currentJob.finishJob(r, true);
                        mirFleet.robots[r].schedule.id = 0;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Done")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CompletedNoErrors;
                        mirFleet.robots[r].schedule.id = 0;
                        mirFleet.robots[r].currentJob.finishMission();
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
    private static int getRobotStatus(int robotID)
    {
        logger(AREA, DEBUG, "Get Robot Status From Robots");

        int restStatus = mirFleet.issueGetRequest("status", robotID);

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
        int waitTime = 50;

        logger(AREA, INFO, "Releasing " + mirFleet.robots[robotID].s.robot_name +  " From The Busy Group");

        //======================================================================|
        // In case the release robot command is driven by a sequence break:     |
        // - Clear any errors from the robot.                                   |
        // - Upnause (put into ready state)                                     |
        //======================================================================|
        mirFleet.robots[robotID].sendRESTdata(mirFleet.robots[robotID].s.putRequest(mirFleet.robots[robotID].getBaseURI()));
        mirFleet.robots[robotID].sendRESTdata(mirFleet.robots[robotID].s.putReadyRequest(mirFleet.robots[robotID].getBaseURI()));

        //======================================================================|
        // Delete from the busy group                                           |
        // This request might not succeed, depending on what happened           |
        //======================================================================|
        restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.busy.deleteRequest(fleetRobotID));

        if(restStatus == Globals.TaskStatus.CompletedNoErrors)
        {
            logger(AREA, INFO, mirFleet.robots[robotID].s.robot_name + " Was Released From Empty Charge Group");

            // We succeeded at deleting the robot from the old charging group
            // Wait for a bit and assign the empty/available charging group
            Thread.Sleep(waitTime);
            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.available.postRequest(fleetRobotID));

            if(restStatus == Globals.TaskStatus.CompletedNoErrors)
            {
                taskStat = Globals.TaskStatus.CompletedNoErrors;
            }
            else
            {
                taskStat = Globals.TaskStatus.FatalError;
                logger(AREA, WARNING, "Failed To Assign " + mirFleet.robots[robotID].s.robot_name + " To Full Charge Group");
            }
        }
        else
        {
            logger(AREA, INFO, "Failed To Remove " + mirFleet.robots[robotID].s.robot_name + " From Empty Charge Group.");

            // We failed to delete the robot from an old charging group
            // If we failed because the robot wasn't in the group, just put it in the charging group
            Thread.Sleep(waitTime);
            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.available.postRequest(fleetRobotID));

            if (restStatus == Globals.TaskStatus.CompletedNoErrors)
            {
                taskStat = Globals.TaskStatus.CompletedNoErrors;
            }
            else
            {
                taskStat = Globals.TaskStatus.FatalError;
                logger(AREA, WARNING, "Failed To Assign " + mirFleet.robots[robotID].s.robot_name + " To Full Charge Group");
            }
        }

        //==================================================================|
        // Need to send a dummy mission so as to prompt the robot           |
        // The robot will enter an "Executing State" and at the end         |
        // it will go back to the idle state.                               |
        // This makes it process the change in charging group               |
        //==================================================================|
        sendMissionToScheduler(Tasks.ReleaseRobot, robotID);

        //==================================================================|
        // Put the robot into the available group. This enables it to       |
        // go charge and also start new jobs - otherwise it's going         |
        // to stick to its current tasks                                    |
        // Available Group ID : 3                                           |
        // TODO: Add a better way to handle this - response from fleet?     |
        // Preferably, fetch the group info at the very begining            |
        //==================================================================|
        try
        {
            sendRobotGroup(robotID, mirFleet.fleet_available_group);
        }
        catch(Exception e)
        {
            logger(AREA, ERROR, "Failed To Put " + mirFleet.robots[robotID].s.robot_name + " Into Available Group");
            logger(AREA, ERROR, "Exception: ", e);
        }

        // Complete the job, save to DB and clear the job ledger
        mirFleet.robots[robotID].currentJob.finishJob(robotID, false);

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

        logger(AREA, INFO, "Occupying " + mirFleet.robots[robotID].s.robot_name + " -> Moving To Busy Group");

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
            sendRobotGroup(robotID, mirFleet.fleet_busy_group);
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "Failed To Put Robot Into Available Group");
            logger(AREA, ERROR, "Exception: ", e);
        }

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

