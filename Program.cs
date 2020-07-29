using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Mirage.plc;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage
{
    class Program
    {
        public static Fleet mirFleet;

        //=========================================================|
        //  Used For Logging                                       |     
        //=========================================================|
        private static readonly Type AREA = typeof(Program);

        public static async Task Main(string[] args)
        {
            readAllSettings();

            connectToDB();

            setUpDefaultComms();

            getInitialFleetData();

            mirFleet.issueGetRequests("status");
            await mirFleet.saveFleetStatusAsync();

            gracefulTermination();

            logger(AREA, DEBUG, "==== Starting Main Loop ====");

            int i = 0;

            //====================================================|
            //  M A I N      L O O P                              |
            //====================================================|
            while (keepRunning)
            {
                int currentTimer = 12345;

                logger(AREA, DEBUG, "==== Loop " + ++i + " Starting ====");

                SiemensPLC.poll();

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

                SiemensPLC.checkConnectivity();

                // Poll MiR Fleet - async operation that happens every pollInterval
                if (currentTimer < pollInterval)
                {
                    try
                    {
                        try
                        {
                            // We're sending GET requests to the MiR servers
                            // Saving them asynchronously as they come along
                            if (debugLevel > -1)
                                Console.WriteLine("==== Getting Status ====");

                            mirFleet.issueGetRequests("status");
                            await mirFleet.saveFleetStatusAsync();

                            if (debugLevel > -1)
                                Console.WriteLine("==== Getting Registers ====");

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
                }

                // Perform calcs + reporting
                //calculationsAndReporting();

                logger(AREA, DEBUG, "==== Loop " + i + " Finished ====");

                //Thread.Sleep(pollInterval*100); // Ugly as fuck but will change to event based stuff once I add a GUI
                //keepRunning = true;
            }

            gracefulTermination();
        }

        /// <summary>
        /// Force the wait so we're doing things in order. Synchronous initial fetch, so there'll be some delay on start-up
        /// </summary>
        private static void getInitialFleetData()
        {
            // Create the fleet which will contain out robot data
            mirFleet = new Fleet();

            try
            {
                try
                {
                    mirFleet.issueGetRequests("/software/logs");
                    mirFleet.saveSoftwareLogsAsync().Wait();
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

            try
            {
                try
                {
                    mirFleet.issueGetRequests("maps");
                    mirFleet.saveMapsAsync().Wait();
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

            try
            {
                try
                {
                    mirFleet.issueGetRequests("settings");
                    mirFleet.saveSettingsAsync().Wait();
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

            try
            {
                try
                {
                    mirFleet.issueGetRequests("settings/advanced");
                    mirFleet.saveSettingsAsync().Wait();
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

            try
            {
                try
                {
                    mirFleet.issueGetRequests("missions");
                    mirFleet.saveMissionsAsync().Wait();
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

            SiemensPLC.updateTaskStatus(Status.CouldntProcessRequest);

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

