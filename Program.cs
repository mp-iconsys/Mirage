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

        //Console.ReadLine();

        initializeFleet();

        //Console.ReadLine();

        //Console.ReadLine();

        //mirFleet.getInitialFleetData();

        logger(AREA, DEBUG, "==== Starting Main Loop ====");

        long i = 0;

        Stopwatch timer = new Stopwatch();
        timer.Start();

        for (int k = 0; k < Globals.sizeOfFleet; k++)
        {
            getRobotStatus(k);
        }

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
                //  Poll the PLC for any tasks                        |
                //====================================================|
                SiemensPLC.poll();

                //Console.ReadLine();

                if (SiemensPLC.newMsg)
                {
                    logger(AREA, DEBUG, "==== New Task From PLC ====");

                    //====================================================|
                    //  Fleet Manager Tasks                               |
                    //====================================================|
                    if (SiemensPLC.newMsgs[0])
                    {
                        SiemensPLC.updateTaskStatus(SiemensPLC.fleetID, Globals.TaskStatus.StartedProcessing);

                        //Console.ReadLine();

                        int taskStatus = Globals.TaskStatus.StartedProcessing;

                        switch (SiemensPLC.fleetBlock.getTaskNumber())
                        {
                            case Tasks.GetScheduleStatus:
                                taskStatus = getScheduleStatus();
                                break;
                            case Tasks.SendMissionToScheduler:
                                taskStatus = sendMissionToScheduler(SiemensPLC.fleetBlock.getTaskParameter()-301, 0);
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
                                taskStatus = getRobotStatus();
                                break;
                            default:
                                taskStatus = unknownMission(SiemensPLC.fleetID);
                                break;
                        }

                        if(taskStatus == Globals.TaskStatus.StartedProcessing)
                        {
                            SiemensPLC.writeFleetBlock(taskStatus);
                        }
                        else
                        {
                            SiemensPLC.writeFleetBlock(Globals.TaskStatus.StartedProcessing);

                            SiemensPLC.writeFleetBlock(taskStatus);
                        }

                        logger(AREA, DEBUG, "Finished Checking Fleet");
                    }
                    else
                    {
                        logger(AREA, DEBUG, "Not A Fleet Data Task");
                    }

                    //Console.ReadLine();

                    //====================================================|
                    //  Robot Tasks                                       |
                    //====================================================|
                    for (int j = 0; j < Globals.sizeOfFleet; j ++)
                    {
                        if (SiemensPLC.newMsgs[j+1])
                        {
                            SiemensPLC.updateTaskStatus(j, Globals.TaskStatus.StartedProcessing);

                            //Settomg Mission Status to Idle
                            mirFleet.robots[j].schedule.state_id = Globals.TaskStatus.Idle;

                            logger(AREA, DEBUG, "Issuing Task For Robot: " + j);
                            logger(AREA, DEBUG, "Task Number: " + SiemensPLC.robots[j].getTaskNumber());

                            int taskStatus = Globals.TaskStatus.StartedProcessing;

                            switch (SiemensPLC.robots[j].getTaskNumber())
                            {
                                case Tasks.GetScheduleStatus:
                                    taskStatus = getScheduleStatus();
                                    break;
                                case Tasks.SendMissionToScheduler:
                                    taskStatus = sendMissionToScheduler(SiemensPLC.robots[j].getTaskParameter() - 301, mirFleet.robotMapping[j]);
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
                                    taskStatus = sendMissionToScheduler(SiemensPLC.robots[j].getTaskParameter() - 301, mirFleet.robotMapping[j]);
                                    break;
                                default:
                                    taskStatus = unknownMission(j);
                                    break;
                            }

                            SiemensPLC.writeRobotBlock(j, taskStatus);

                            SiemensPLC.newMsgs[j + 1] = false;

                            //Console.ReadLine();
                        }
                    }

                    logger(AREA, DEBUG, "==== Completed Processing Tasks ====");
                }

                SiemensPLC.checkResponse();

                //====================================================|
                //  Read Alarms And Flag If Triggered                 |
                //====================================================|
                //SiemensPLC.readAlarms();

                getRobotGroups();

                checkMissionAssignment();

                //Console.ReadLine();

                //====================================================|
                //  Fetch High Frequency Data for the PLC             |
                //====================================================|
                for (int k = 0; k < Globals.sizeOfFleet; k++)
                {
                    getRobotStatus(k);

                    getRobotStatusFromFleet(k);
                }
            }
            else
            {
                logger(AREA, ERROR, "==== PLC Is Not Connected ====");
            }

            SiemensPLC.checkConnectivity();

            // Poll MiR Fleet - async operation that happens every pollInterval
            if (timer.Elapsed.Seconds >= pollInterval)
            {
                calculationsAndReporting();

                logger(AREA, INFO, timer.Elapsed.TotalSeconds + " seconds since last poll. Poll interval is: " + pollInterval);

                timer.Restart();
                mirFleet.pollRobots();
            }

            checkConfigChanges();

            logger(AREA, DEBUG, "==== Loop " + i + " Finished ====");

            //Console.ReadLine();

            for(int g = 0; g < sizeOfFleet+1; g++)
            {
                logger(AREA, DEBUG, "MESSAGE STATUS: " + SiemensPLC.newMsgs[g]);
            }

            Thread.Sleep(1000); // Remove in live deployment
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
    /// Sends a mission to Fleet Scheduler. If the robotID is 0, it sends a new mision and checks which robot got assigned
    /// </summary>
    private static int sendMissionToScheduler(int mission_number, int robotID)
    {
        int restStatus;

        logger(AREA, INFO, "==== Send Mission To Scheduler ====");
        logger(AREA, INFO, "Mapped Robot ID: " + robotID);
        logger(AREA, INFO, "Mission No: " + mission_number + " For robot: " + robotID);

        mirFleet.fleetManager.Missions[mission_number].print();

        if (robotID == 0)
        {
            logger(AREA, INFO, "==== Sending Brand New Mission To Any Robots ====");

            //if()
            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[mission_number].postRequest());

            Thread.Sleep(2000);

            // Check the mission_scheduler we've just created to return with the robot ID
            if (mirFleet.fleetManager.schedule.working_response)
            {
                int currentMissionRobot = 0;

                logger(AREA, DEBUG, "Mission Scheduler Robot ID is: " + mirFleet.fleetManager.schedule.robot_id);
                logger(AREA, DEBUG, "Mission Schedule ID is: " + mirFleet.fleetManager.schedule.id);

                restStatus = mirFleet.issueGetRequest("mission_scheduler/" + mirFleet.fleetManager.schedule.id, 666);

                logger(AREA, DEBUG, "Mission Scheduler Robot ID After polling is: " + mirFleet.fleetManager.schedule.robot_id);

                if (restStatus == Globals.TaskStatus.CompletedNoErrors && mirFleet.fleetManager.schedule.robot_id != 0)
                {
                    //currentMissionRobot = mirFleet.fleetManager.schedule.robot_id - 1; // This is as the robots are offset by one in fleet - change to be better
                    currentMissionRobot = mirFleet.fleetManager.schedule.robot_id - 3; // TODO: This is as the robots are offset by one in fleet - change to be better
                    mirFleet.returnParameter = (short)(mirFleet.fleetManager.schedule.robot_id - 2);

                    for (int i = 0; i < sizeOfFleet; i++)
                    {
                        if (currentMissionRobot == i)
                        {
                            mirFleet.robots[i].schedule.id = mirFleet.fleetManager.schedule.id;
                        }
                    }

                    mirFleet.fleetManager.schedule.working_response = false;
                }
                else
                {
                    restStatus = Globals.TaskStatus.StartedProcessing;
                    mirFleet.fleetManager.schedule.working_response = true;
                }

                //Console.ReadLine();
            }

            //SiemensPLC.updateTaskStatus(SiemensPLC.fleetID, restStatus);
        }
        else
        {
            logger(AREA, INFO, "==== Sending A Follow-Up Mission To Robot: " + robotID + " ====");

            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[mission_number].postRequest(robotID+2));

            mirFleet.robots[robotID - 1].schedule.id++;

            SiemensPLC.updateTaskStatus(robotID-1, Globals.TaskStatus.StartedProcessing);
        }

        logger(AREA, DEBUG, "==== Mission Sent ====");
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
    private static int sendRobotGroup()
    {
        logger(AREA, INFO, "==== Send (Change) Robot Group To Scheduler ====");

        int robotID = 1;
        int robotGroupID = 2;

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
        logger(AREA, INFO, "==== Scan Fleet Manager For Robot Groups ====");

        // Need to add whether to turn alarm on/off and which alarm to affect from PLC
        int restStatus = mirFleet.issueGetRequest("robots?whitelist=robot_group_id", SiemensPLC.fleetID);

        fleetMemoryToPLC();
        SiemensPLC.writeFleetBlock(SiemensPLC.fleetBlock.getTaskStatus());
        //int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Group.putRequest(robotID, robotGroupID));

        //SiemensPLC.updateTaskStatus(restStatus);
        logger(AREA, DEBUG, "Status: " + restStatus);
        logger(AREA, DEBUG, "==== Fetched Robot Groups ====");
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

    private static void checkMissionAssignment()
    {
        logger(AREA, INFO, "==== Checking Mission Assignment ====");

        if(mirFleet.fleetManager.schedule.working_response)
        {
            // Still waiting for the Fleet Mission to be assigned
            // Check the mission_scheduler we've just created to return with the robot ID
                int currentMissionRobot = 0;
                int restStatus = 0;

                logger(AREA, DEBUG, "Current Scheduler Robot ID is: " + mirFleet.fleetManager.schedule.robot_id);
                logger(AREA, DEBUG, "Current Mission Schedule ID is: " + mirFleet.fleetManager.schedule.id);

                restStatus = mirFleet.issueGetRequest("mission_scheduler/" + mirFleet.fleetManager.schedule.id, 666);

                logger(AREA, DEBUG, "Mission Scheduler Robot ID After polling is: " + mirFleet.fleetManager.schedule.robot_id);

                if (restStatus == Globals.TaskStatus.CompletedNoErrors && mirFleet.fleetManager.schedule.robot_id != 0)
                {
                    currentMissionRobot = mirFleet.fleetManager.schedule.robot_id - 3; // TODO: This is as the robots are offset by one in fleet - change to be better
                    mirFleet.returnParameter = (short)(mirFleet.fleetManager.schedule.robot_id-2);

                    for (int i = 0; i < sizeOfFleet; i++)
                    {
                        if (currentMissionRobot == i)
                        {
                            mirFleet.robots[i].schedule.id = mirFleet.fleetManager.schedule.id;
                        }
                    }

                restStatus = Globals.TaskStatus.CompletedNoErrors;

                mirFleet.fleetManager.schedule.working_response = false;
                }
                else
                {
                    restStatus = Globals.TaskStatus.StartedProcessing;
                    mirFleet.fleetManager.schedule.working_response = true;
                }

            SiemensPLC.writeFleetBlock(restStatus);

        }
        else
        {
            // Check the mission status for each robot

            for(int r = 0; r < sizeOfFleet; r++)
            {
                int restStatus = 0;

                if(mirFleet.robots[r].schedule.id != 0)
                {
                    // Check the status of the mission assigned to robot r
                    logger(AREA, DEBUG, "Mission Scheduler Robot ID is: " + mirFleet.fleetManager.schedule.robot_id);
                    logger(AREA, DEBUG, "Mission Schedule ID is: " + mirFleet.robots[r].schedule.id);

                    restStatus = mirFleet.checkMissionSchedule("mission_scheduler/" + mirFleet.robots[r].schedule.id + "?whilelist=state", SiemensPLC.fleetID, r);

                    logger(AREA, DEBUG, "Mission Status For Robot: " + r + " is: " + mirFleet.robots[r].schedule.state);

                    if(mirFleet.robots[r].schedule.state == "Pending")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }
                    else if(mirFleet.robots[r].schedule.state == "Executing")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Outbound")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }
                    else if (mirFleet.robots[r].schedule.state == "Done")
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.CompletedNoErrors;
                    }
                    else
                    {
                        mirFleet.robots[r].schedule.state_id = Globals.TaskStatus.StartedProcessing;
                    }

                    logger(AREA, DEBUG, "Mission Status For Robot: " + r + " State ID is: " + mirFleet.robots[r].schedule.state_id);
                }
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
    private static int getRobotStatus()
    {
        logger(AREA, INFO, "==== Get Mission Status ====");

        int restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

        SiemensPLC.writeData("mission_text", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);

        logger(AREA, DEBUG, "==== Mission Status Fetched And Saved ====");

        return restStatus;
    }

    /// <summary>
    /// 
    /// </summary>
    private static int getRobotStatus(int robotID)
    {
        logger(AREA, INFO, "==== Get Robot Status ====");

        int restStatus = mirFleet.issueGetRequest("status", robotID);

        logger(AREA, INFO, "==== Got A Response ====");

        SiemensPLC.writeRobotBlock(robotID);

        logger(AREA, DEBUG, "==== Robot Status Fetched And Saved ====");

        return restStatus;
    }

    private static int getRobotStatusFromFleet(int robotID)
    {
        logger(AREA, INFO, "==== Get Robot Status ====");

        int restStatus = mirFleet.issueGetRequest("robotStatusFromFleet", robotID);

        SiemensPLC.writeRobotBlock(robotID);

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
    /// 
    /// </summary>
    private static void calculationsAndReporting()
    {
        reports.reportingPass();
    }
}

