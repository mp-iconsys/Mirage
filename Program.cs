using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Mirage.plc;
using static Globals;

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

            connectToDB();

            setUpDefaultComms();

            // Create the fleet which will contain out robot data
            mirFleet = new Fleet();

            getInitialFleetData();

            if (debugLevel > -1)
                Console.WriteLine("==== Starting Main Loop ====");

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
                    Console.WriteLine("==== New Task From PLC ====");

                    // Set PLC status to Mirage processing
                    SiemensPLC.updateTaskStatus(Status.StartedProcessing);
                    int restStatus = Status.CompletedNoErrors;

                    // Check which task we've got to do & do it
                    // Save any response to predefined PLC registers
                    switch (SiemensPLC.task)
                    {
                        case Tasks.SchedulerStatus:
                            Console.WriteLine("==== Get Schedule Status ===="); // obtained from the fleet
                            restStatus = mirFleet.issueGetRequest("mission_scheduler/" + SiemensPLC.robotID, SiemensPLC.robotID);
                            SiemensPLC.writeStringData("mission_text", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);
                            break;
                        case Tasks.SchedulerSendMission:
                            Console.WriteLine("==== Send Mission To Scheduler ====");
                            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.m.postRequest());
                            SiemensPLC.updateTaskStatus(restStatus);
                            break;
                        case Tasks.SchedulerCreateMission:
                            Console.WriteLine("==== Create New Mission In Scheduler ====");
                            // create mission using /v2.0.0/missions + guuid
                            // uses POST

                            //status = sendRESTdata((testMission.create_mission(Int32.Parse(incomingMessage.Paramater))));
                            //string resp;

                            //if (status < 400)
                            //    resp = "Success";
                            //else
                            //    resp = status.ToString();

                            //outgoingMessage.saveMessage(incomingMessage.SerialNumber, resp, null, null);

                            ////outgoingMessage.saveMessage(incomingMessage.SerialNumber, "MISSION", incomingMessage.Paramater, status.ToString());
                            //Console.WriteLine(outgoingMessage.returnMsg());
                            break;
                        case Tasks.SchedulerClear:
                            Console.WriteLine("==== Clear Mission Schedule ====");
                            restStatus = mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.m.deleteRequest());
                            SiemensPLC.updateTaskStatus(restStatus);
                            break;
                        case Tasks.Battery:
                            Console.WriteLine("==== Get Battery Life ====");    // "battery_percentage" from robot status
                            restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);
                            SiemensPLC.writeFloatData("battery", restStatus, mirFleet.robots[SiemensPLC.robotID].s.battery_percentage);
                            break;
                        case Tasks.Distance:
                            Console.WriteLine("==== Get Distance Travelled ====");  // "moved" from robot status
                            restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);
                            SiemensPLC.writeFloatData("moved", restStatus, mirFleet.robots[SiemensPLC.robotID].s.moved);
                            break;
                        case Tasks.RobotStatus:
                            Console.WriteLine("==== Get Mission Status ====");  // "mission_text" from robot status
                            restStatus = mirFleet.issueGetRequest("status", SiemensPLC.robotID);
                            SiemensPLC.writeStringData("mission_text", restStatus, mirFleet.robots[SiemensPLC.robotID].s.mission_text);
                            break;
                        default:
                            Console.WriteLine("==== Unknown Mission ====");
                            SiemensPLC.updateTaskStatus(Status.CouldntProcessRequest);
                            // Issue an alert
                            break;
                    }

                    // Check PLC parsed the data alright
                    // Times out after a while if no respose
                    SiemensPLC.checkResponse();
                }

                // Poll MiR Fleet - async operation
                // happens every pollInterval
                if (currentTimer < pollInterval)
                {
                    try
                    {
                        try
                        {
                            // We're sending GET requests to the MiR servers
                            // Saving them asynchronously as they come along

                            Console.WriteLine("==== Getting Status ====");
                            mirFleet.issueGetRequests("status");
                            await mirFleet.saveFleetStatusAsync();

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


                // Alert and error check
                checkAlertsAndErrors();

                Console.WriteLine("==== Loop " + i + " Finished ====");
                //Thread.Sleep(Globals.pollInterval*1000); // Ugly as fuck but will change to event based stuff once I add a GUI
            }
            
            closeComms();

            Console.WriteLine("==== Graceful Exit ====");

            Environment.Exit(1);
        }

        // Force the wait so we're doing things in order
        // Synchronous initial fetch, so there'll be some delay on start-up
        public static void getInitialFleetData()
        {
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

    }
}

