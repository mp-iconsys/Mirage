﻿using System;
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

        mirFleet.getInitialFleetData();

        setUpDefaultComms();

        Console.WriteLine("Enter SerialNumber: ");

        logger(AREA, DEBUG, "==== Starting Main Loop ====");

        long i = 0;

        Stopwatch timer = new Stopwatch();
        timer.Start();

        //====================================================|
        //  M A I N      L O O P                              |
        //====================================================|
        while (keepRunning)
        {
            logger(AREA, DEBUG, "==== Loop " + ++i + " Starting ====");
            logger(AREA, DEBUG, "Current Stopwatch Time: " + timer.Elapsed.TotalSeconds);

            if (SiemensPLC.plcConnected)
            {
                SiemensPLC.poll();

                if (SiemensPLC.newMsg)
                {
                    logger(AREA, DEBUG, "==== New Task From PLC ====");

                    SiemensPLC.updateTaskStatus(Globals.TaskStatus.StartedProcessing);

                    if (SiemensPLC.fleetBlock.getTaskStatus() == Globals.TaskStatus.StartedProcessing)
                    {
                        switch (SiemensPLC.fleetBlock.getTaskNumber())
                        {
                            case Tasks.GetScheduleStatus:
                                getScheduleStatus();
                                break;
                            case Tasks.SendMissionToScheduler:
                                sendMissionToScheduler();
                                break;
                            case Tasks.CreateMission:
                                createMission();
                                break;
                            case Tasks.ClearScheduler:
                                clearScheduler();
                                break;
                            case Tasks.GetBattery:
                                getBattery();
                                break;
                            case Tasks.GetDistance:
                                getDistance();
                                break;
                            case Tasks.GetRobotStatus:
                                getRobotStatus();
                                break;
                            default:
                                unknownMission();
                                break;
                        }
                    }

                    for (int j = 0; j < Globals.sizeOfFleet; j ++)
                    {
                        if (SiemensPLC.robots[j].getTaskStatus() == Globals.TaskStatus.StartedProcessing)
                        {
                            switch (SiemensPLC.robots[j].getTaskNumber())
                            {
                                case Tasks.GetScheduleStatus:
                                    getScheduleStatus();
                                    break;
                                case Tasks.SendMissionToScheduler:
                                    sendMissionToScheduler();
                                    break;
                                case Tasks.CreateMission:
                                    createMission();
                                    break;
                                case Tasks.ClearScheduler:
                                    clearScheduler();
                                    break;
                                case Tasks.GetBattery:
                                    getBattery();
                                    break;
                                case Tasks.GetDistance:
                                    getDistance();
                                    break;
                                case Tasks.GetRobotStatus:
                                    getRobotStatus();
                                    break;
                                default:
                                    unknownMission();
                                    break;
                            }
                        }
                    }

                    SiemensPLC.checkResponse();
                }

                // Now fetch data for the PLC
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

            Thread.Sleep(500); // Remove in live deployment
        }

        gracefulTermination();
    }

    /// <summary>
    /// 
    /// </summary>
    private static void getScheduleStatus()
    {
        logger(AREA, INFO, "==== Get Schedule Status ====");

        int restStatus = mirFleet.issueGetRequest("mission_scheduler/" + SiemensPLC.robotID, SiemensPLC.robotID);

        SiemensPLC.writeData("mission_schedule", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);

        logger(AREA, DEBUG, "==== Obtained Scheduler Status ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void getRobotGroup()
    {
        logger(AREA, INFO, "==== Get Robot Group Data ====");

        int restStatus = mirFleet.issueGetRequest("robots/" + 0, 666);
        mirFleet.fleetManager.Group.print();

        //SiemensPLC.writeData("mission_schedule", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);

        logger(AREA, DEBUG, "==== Obtained Scheduler Status ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void sendMissionToScheduler()
    {
        logger(AREA, INFO, "==== Send Mission To Scheduler ====");

        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[0].postRequest());

        SiemensPLC.updateTaskStatus(restStatus);

        logger(AREA, DEBUG, "==== Mission Sent ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void sendFireAlarm()
    {
        logger(AREA, INFO, "==== Send Fire Alarm To Scheduler ====");

        // Need to add whether to turn alarm on/off and which alarm to affect from PLC
        //int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.FireAlarm.putRequest(Program.alarm_on, 1));

        //SiemensPLC.updateTaskStatus(restStatus);
        //logger(AREA, DEBUG, "Status: " + restStatus);
        logger(AREA, DEBUG, "==== Fire Alarm Sent ====");

        //Program.alarm_on = !Program.alarm_on;
    }

    /// <summary>
    /// 
    /// </summary>
    private static void sendRobotGroup()
    {
        logger(AREA, INFO, "==== Send (Change) Robot Group To Scheduler ====");

        int robotID = 1;
        int robotGroupID = 2;

        // Need to add whether to turn alarm on/off and which alarm to affect from PLC
        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Group.putRequest(robotID, robotGroupID));

        //SiemensPLC.updateTaskStatus(restStatus);
        logger(AREA, DEBUG, "Status: " + restStatus);
        logger(AREA, DEBUG, "==== Robot Group Changed ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void createMission()
    {
        logger(AREA, INFO, "==== Create New Mission In Robot " + SiemensPLC.robotID + " ====");

        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[0].createMission(SiemensPLC.parameter));

        SiemensPLC.updateTaskStatus(restStatus);

        logger(AREA, DEBUG, "==== Created New Mission ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void clearScheduler()
    {
        logger(AREA, INFO, "==== Clear Mission Schedule ====");

        int restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[0].deleteRequest());

        SiemensPLC.updateTaskStatus(restStatus);

        logger(AREA, DEBUG, "==== Cleared Mission Scheduler ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void getBattery()
    {
        Console.WriteLine("==== Get Battery Life ====");

        int restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

        SiemensPLC.writeData("battery", restStatus, mirFleet.robots[SiemensPLC.robotID].s.battery_percentage);
    }

    /// <summary>
    /// 
    /// </summary>
    private static void getDistance()
    {
        logger(AREA, INFO, "==== Get Distance Travelled ====");

        int restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

        SiemensPLC.writeData("moved", restStatus, mirFleet.robots[SiemensPLC.robotID].s.moved);

        logger(AREA, DEBUG, "==== Distance Travelled Status Fetched And Saved ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void getRobotStatus()
    {
        logger(AREA, INFO, "==== Get Mission Status ====");

        int restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

        SiemensPLC.writeData("mission_text", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);

        logger(AREA, DEBUG, "==== Mission Status Fetched And Saved ====");
    }

    /// <summary>
    /// 
    /// </summary>
    private static void unknownMission()
    {
        logger(AREA, ERROR, "==== Unknown Mission ====");

        SiemensPLC.updateTaskStatus(Globals.TaskStatus.CouldntProcessRequest);

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

