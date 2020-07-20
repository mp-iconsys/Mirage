using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Mirage.plc;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage
{
    class Program
    {
        public static Fleet mirFleet;

        public static async Task Main(string[] args)
        {
            // Capture CTRL+C for gracefull termination
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) 
            {
                e.Cancel = true;
                keepRunning = false;
            };

            // TODO: Capture seg faults 

            // TODO: Thread diagnostics???

            readAllSettings();

            Environment.Exit(1);

            connectToDB();

            setUpDefaultComms();

            getInitialFleetData();

            if (debugLevel > -1)
            { 
                Console.WriteLine("==== Starting Main Loop ====");
                logger(typeof(Program), INFO, "==== Starting Main Loop ====");
            }

            int i = 0;

            //============================================= 
            // M A I N      L O O P
            //============================================= 
            while (keepRunning)
            {
                int currentTimer = 0;

                if(debugLevel > -1)
                    Console.WriteLine("==== Loop " + ++i + " Starting ====");

                // Poll PLC for status changes
                SiemensPLC.poll();

                // Act on PLC data
                // This is synchronous even though it involves http requests
                if(SiemensPLC.newMsg)
                {
                    if (debugLevel > -1)
                        Console.WriteLine("==== New Task From PLC ====");

                    // Set PLC status to Mirage processing
                    SiemensPLC.updateTaskStatus(Status.StartedProcessing);

                    // Check which task we've got to do & do it
                    // Save any response to predefined PLC registers
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

                    // Check PLC parsed the data alright
                    // Times out after a while if no respose
                    SiemensPLC.checkResponse();
                }

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
                calculationsAndReporting();

                // Alert and error check
                checkAlertsAndErrors();

                Console.WriteLine("==== Loop " + i + " Finished ====");

                Thread.Sleep(Globals.pollInterval*1000); // Ugly as fuck but will change to event based stuff once I add a GUI
            }
            
            closeComms();

            if (debugLevel > -1)
                Console.WriteLine("==== Graceful Exit ====");

            Environment.Exit(1);
        }

        // Force the wait so we're doing things in order
        // Synchronous initial fetch, so there'll be some delay on start-up
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


            // Download missions
        }

        private static void getScheduleStatus()
        {
            Console.WriteLine("==== Get Schedule Status ====");

            int restStatus = Status.CompletedNoErrors;
                restStatus = mirFleet.issueGetRequest("mission_scheduler/" + SiemensPLC.robotID, SiemensPLC.robotID);

            SiemensPLC.writeStringData("mission_text", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);
        }

        private static void sendMissionToScheduler()
        {
            Console.WriteLine("==== Send Mission To Scheduler ====");

            int restStatus = Status.CompletedNoErrors;
                restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.m.postRequest());

            SiemensPLC.updateTaskStatus(restStatus);
        }

        private static void createMission()
        {
            Console.WriteLine("==== Create New Mission In Robot " + SiemensPLC.robotID + " ====");

            int restStatus = Status.CompletedNoErrors;
                restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.m.createMission(SiemensPLC.parameter));

            SiemensPLC.updateTaskStatus(restStatus);
        }

        private static void clearScheduler()
        {
            Console.WriteLine("==== Clear Mission Schedule ====");

            int restStatus = Status.CompletedNoErrors;
                restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.m.deleteRequest());

            SiemensPLC.updateTaskStatus(restStatus);
        }

        private static void getBattery()
        {
            Console.WriteLine("==== Get Battery Life ====");

            int restStatus = Status.CompletedNoErrors;
                restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

            SiemensPLC.writeFloatData("battery", restStatus, mirFleet.robots[SiemensPLC.robotID].s.battery_percentage);
        }

        private static void getDistance()
        {
            Console.WriteLine("==== Get Distance Travelled ====");

            int restStatus = Status.CompletedNoErrors;
                restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

            SiemensPLC.writeFloatData("moved", restStatus, mirFleet.robots[SiemensPLC.robotID].s.moved);
        }

        private static void getRobotStatus()
        {
            Console.WriteLine("==== Get Mission Status ====");

            int restStatus = Status.CompletedNoErrors;
                restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);

            SiemensPLC.writeStringData("mission_text", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);
        }

        private static void unknownMission()
        {
            Console.WriteLine("==== Unknown Mission ====");
            SiemensPLC.updateTaskStatus(Status.CouldntProcessRequest);

            // Issue an alert
        }


        private static void calculationsAndReporting()
        {

        }
    }
}

