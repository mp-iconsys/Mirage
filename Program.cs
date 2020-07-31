using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mirage.plc;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage
{
    class Program
    {
        //=========================================================|
        //  Used For Logging                                       |     
        //=========================================================|
        private static readonly Type AREA = typeof(Program);

        public static async Task Main(string[] args)
        {
            readAllSettings();

            connectToDB();

            setUpDefaultComms();

            mirFleet.getInitialFleetData();

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

                /*                SiemensPLC.poll();

                                if (SiemensPLC.newMsg)
                                {
                                    logger(AREA, DEBUG, "==== New Task From PLC ====");

                                    SiemensPLC.updateTaskStatus(Status.StartedProcessing);

                                    switch (SiemensPLC.task)
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

                                    SiemensPLC.checkResponse();
                                }

                                SiemensPLC.checkConnectivity();*/

                // Poll MiR Fleet - async operation that happens every pollInterval
                if (timer.Elapsed.Seconds >= pollInterval)
                {
                    logger(AREA, INFO, timer.Elapsed.TotalSeconds + " seconds since last poll. Poll interval is: " + pollInterval);
                    timer.Restart();
                    mirFleet.pollRobots();
                }

                calculationsAndReporting();

                logger(AREA, DEBUG, "==== Loop " + i + " Finished ====");

                Thread.Sleep(500);
            }

            gracefulTermination();
        }

        /*        public static async void pollRobots()
                {
                    try
                    {
                        try
                        { 
                            mirFleet.issueGetRequests("status");
                            await mirFleet.saveFleetStatusAsync();

                            mirFleet.issueGetRequests("registers");
                            await mirFleet.saveFleetRegistersAsync();
                        }
                        catch (HttpRequestException e)
                        {
                            // TODO: Handle more exceptions
                            // Remove the task which is causing the exception

                            Console.WriteLine("Couldn't connect to the robot");
                            Console.WriteLine("Check your network, dns settings, robot is up, etc.");
                            Console.WriteLine("Please see error log (enter location here) for more details");
                            // Store the detailed error in the error log
                            Console.WriteLine(e);
                        }
                    }
                    catch (WebException e)
                    {
                        Console.WriteLine($"Connection Problems: '{e}'");
                    }
                }*/



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

        }
    }
}

