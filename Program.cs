﻿using System;
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
                            Console.WriteLine("==== Get Schedule Status ====");
                            // response = getRESTdata("mission_scheduler/" + incomingMessage.Paramater);
                            // obtained from the fleet
                            break;
                        case Tasks.SchedulerSendMission:
                            Console.WriteLine("==== Send Mission To Scheduler ====");
                            // done to the fleet
                            //status = sendRESTdata((testMission.send_mission(Int32.Parse(incomingMessage.Paramater))));
                            //outgoingMessage.saveMessage(incomingMessage.SerialNumber, "MISSION", incomingMessage.Paramater, status.ToString());
                            //Console.WriteLine(outgoingMessage.returnMsg());
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
                            restStatus = mirFleet.clearSchedule();
                            // done to the fleet mission_scheduler
                            //status = sendRESTdata((testMission.clear_mission_schedule()));
                            //outgoingMessage.saveMessage(incomingMessage.SerialNumber, "POLL", incomingMessage.Paramater, status.ToString());
                            //Console.WriteLine(outgoingMessage.returnMsg());
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

        // Get a JSON object based on an uri and an open client connection
        /*
        public async Task<JObject> GetAsync(string uri)
        {
            var response = await Client.GetAsync(uri);

            //will throw an exception if not successful
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            return await Task.Run(() => JObject.Parse(content));
        }

        // post a JSON object based on an uri and an open client connection
        public async Task<JObject> PostAsync(string uri, string data)
        {
            var response = await Client.PostAsync(uri, new StringContent(data));

            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync();
            return await Task.Run(() => JObject.Parse(content));
        }
        */

        // We're assuming here that the REST API will use the same format accross all of the robots
        // NOTE! This does not actually connect to the robots! Just sets up default values
        // Actual connection is done through the Robot class method, once we know the IP + authentication

        /*
        static void httpResponseCheck(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                if(debugLevel > 0)
                { 
                    Console.WriteLine("HTTP Response Was Successfull");
                    Console.WriteLine(response.StatusCode);
                }
            }
            else
            {
                // Print error here
                // Send email cause of critical error
            }
        }
        */

        /*
        static void ReadSetting(string key)
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string result = appSettings[key] ?? "Not Found";
                Console.WriteLine(result);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }
        }


        */

        /*
        static AuthenticationHeaderValue fetchAuthentication()
        {
            // Basic Auth type for the API. Set up as follows: BASE64( username: sha256(pass) )
            // So, first get sha256 of the pass, Concat to "username:" and then do base64 conversion
            Console.WriteLine("Enter API Username:");
            apiUsername = Console.ReadLine();

            Console.WriteLine("Enter API Password:");
            apiPassword = Console.ReadLine();

            return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiUsername}:{ComputeSha256Hash(apiPassword)}")));
        }
        */

        /*
        void run()
        {
            CancellationTokenSource cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(80));
            Console.WriteLine("==== Starting action loop ====");
            RepeatActionEvery( () => Console.WriteLine("Action"), TimeSpan.FromSeconds(10), cancellation.Token ).Wait();
            Console.WriteLine("==== Finished action loop ====");
        }

        public static async Task RepeatActionEvery(Action action, TimeSpan interval, CancellationToken cancellationToken)
        {
            while (true)
            {
                action();
                Task task = Task.Delay(interval, cancellationToken);

                try
                {
                    await task;
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }
        */

        /*
        private async static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Polling @ {0}", e.SignalTime);

            try
            {
                try
                {
                    // We're sending GET requests to the MiR servers
                    // Saving them asynchronously as they come along

                    //Console.WriteLine("==== Getting Status ====");

                    mirFleet.issueGetRequests("status");

                    await mirFleet.saveFleetStatusAsync();

                    //Console.WriteLine("==== Getting Registers ====");

                    //mirFleet.issueGetRequests("registers");

                    //await mirFleet.saveFleetRegistersAsync();
                }
                catch (HttpRequestException exp)
                {
                    // TODO: Handle more exceptions
                    // Remove the task which is causing the exception

                    Console.WriteLine("Couldn't connect to the robot");
                    Console.WriteLine("Check your network, dns settings, robot is up, etc.");
                    Console.WriteLine("Please see error log (enter location here) for more details");
                    // Store the detailed error in the error log
                    Console.WriteLine(exp);
                }
            }
            catch (WebException exp)
            {
                Console.WriteLine($"Connection Problems: '{exp}'");
            }
        }
        */

        // This sends an async API get request to the robot to fetch data at the specified uri
        // It does not return data straight away. This allows us to make a bunch of calls
        // For all of the robots and then wait for the data to get to us as it comes through.
        //public static HttpResponseMessage getRESTdata(string uri)
        //{
        //    //return comms.GetAsync(uri).Result;
        //}

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

        public static int sendRESTdata(HttpRequestMessage request)
        {
            int statusCode = 0;

            //HttpResponseMessage result = comms.SendAsync(request).Result;

            //if (result.IsSuccessStatusCode)
            //{
            //    statusCode = (int)result.StatusCode;

            //    if (statusCode > 199 && statusCode < 400)
            //    {
            //        Console.WriteLine("Data Sent Successfully");
            //    }
            //    else if (statusCode > 399)
            //    {
            //        Console.WriteLine("Data send did not succeed");
            //    }
            //    else
            //    {
            //        Console.WriteLine("Unknown Error");
            //    }
            //}

            return statusCode;
        }

    }
}

