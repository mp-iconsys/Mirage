using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage
{
    public class Fleet
    {
        //=========================================================|
        // Fleet manager is essentially a robot                    |
        // keep it separate from the robot array for cleaner code  |
        //=========================================================|
        // TODO : Make these all lists, so the size can be amended at runtime
        public Robot[] robots;
        public Robot fleetManager;
        private Task<HttpResponseMessage>[] httpResponseTasks;
        private Task<HttpResponseMessage> fleetResponseTask;

        public short returnParameter;
        public short[] groups;

        //=========================================================|
        //  Used For Debugging                                     |     
        //=========================================================|
        private static readonly Type AREA = typeof(Fleet);

        /// <summary>
        /// Initializes the robot fleet, Fleet Manager excluded
        /// </summary>
        /// /// <param name="sizeOfFleet">Number of robots in the fleet</param>
        public Fleet(int sizeOfFleet)
        {
            robots = new Robot[sizeOfFleet];
            httpResponseTasks = new Task<HttpResponseMessage>[sizeOfFleet];

            // Instantiates the group array
            groups = new short[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

            instantiateRobots(sizeOfFleet);
        }

        /// <summary>
        /// Initializes the robot fleet including the Fleet Manager
        /// </summary>
        /// <param name="sizeOfFleet">Number of robots in the fleet</param>
        /// <param name="fleetManagerIP">The IP of the Fleet Manager</param>
        /// <param name="fleetManagerAuthToken">Authentication Token of the Fleet Manager</param>
        public Fleet(int sizeOfFleet, string fleetManagerIP, AuthenticationHeaderValue fleetManagerAuthToken)
        {
            robots = new Robot[sizeOfFleet];
            httpResponseTasks = new Task<HttpResponseMessage>[sizeOfFleet];

            logger(AREA, DEBUG, "Assigned Basics");

            instantiateRobots(sizeOfFleet, fleetManagerIP, fleetManagerAuthToken);
        }

        /// <summary>
        /// Instantiates the mir fleet without the fleet manager.
        /// </summary>
        /// <param name="sizeOfFleet"></param>
        public void instantiateRobots(int sizeOfFleet)
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                // Instantiate the robots - Don't touch the tasks yet
                robots[i] = new Robot(i);
            }
        }

        /// <summary>
        /// Instantiates the mir fleet including the fleet manager.
        /// </summary>
        /// <param name="sizeOfFleet">Number of robots in the fleet</param>
        /// <param name="fleetManagerIP">The IP of the Fleet Manager</param>
        /// <param name="fleetManagerAuthToken">Authentication Token of the Fleet Manager</param>
        public void instantiateRobots(int sizeOfFleet, string fleetManagerIP, AuthenticationHeaderValue fleetManagerAuthToken)
        {
            fleetManager = new Robot(fleetManagerIP, fleetManagerAuthToken);

            logger(AREA, DEBUG, "Completed Fleet Assignment");

            for (int i = 0; i < sizeOfFleet; i++)
            {
                // Instantiate the robots - Don't touch the tasks yet
                robots[i] = new Robot(i);
            }

            logger(AREA, DEBUG, "Completed Robot Assignment");
        }

        /// <summary>
        /// Issues HTTP Get Requests to all MiR robots in memory.
        /// </summary>
        /// <param name="type">Defines the request type</param>
        public void issueGetRequests(string type)
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                try
                {
                    try
                    {
                        httpResponseTasks[i] = robots[i].sendGetRequest(type);
                    }
                    catch (HttpRequestException exception)
                    {
                        // TODO: Remove the task which is causing the exception
                        logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                        logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                    }
                }
                catch (System.Net.WebException exception)
                {
                    logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
                }
            }
        }

        /// <summary>
        /// Sync method that issues a get request and saves data in memory. Used to issue tasks as a middleman for the PLC.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="robotID"></param>
        /// <returns></returns>
        public int issueGetRequest(string type, int robotID)
        {
            logger(AREA, DEBUG, "==== Issuing Get Request ====");

            int functionStatus = Globals.TaskStatus.CompletedNoErrors;

            try
            {
                try
                {
                    if (type == "mission_scheduler" || robotID == 666)
                    {
                        logger(AREA, DEBUG, "Sending " + type + " To Fleet Manager");
                        fleetResponseTask = fleetManager.sendGetRequest(type);
                    }
                    else if(type == "robots" || robotID == 666)
                    {
                        logger(AREA, DEBUG, "Sending " + type + " To Fleet Manager");
                        fleetResponseTask = fleetManager.sendGetRequest(type);
                    }
                    else
                    {
                        logger(AREA, DEBUG, "Sending " + type + " To Robot No " + robotID);
                        httpResponseTasks[robotID] = robots[robotID].sendGetRequest(type);
                    }
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);

                    functionStatus = Globals.TaskStatus.CouldntProcessRequest;
                }
            }
            catch (System.Net.WebException exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);

                functionStatus = Globals.TaskStatus.CouldntProcessRequest;
            }

            try
            {
                if (type == "mission_scheduler" || robotID == 666)
                {
                    fleetResponseTask.Wait();
                }
                else
                {
                    httpResponseTasks[robotID].Wait();

                    logger(AREA, DEBUG, "Waiting For HTTP Response Task");
                }
            }
            catch (Exception exception)
            {
                functionStatus = Globals.TaskStatus.CouldntProcessRequest;

                logger(AREA, ERROR, "Connection Error: ", exception);
            }

            if (type == "status")
            {
                robots[robotID].saveStatusInMemory(httpResponseTasks[robotID].Result);

                logger(AREA, DEBUG, "Status is : " + robots[robotID].s.mission_text);
            }
            else if (type == "mission_scheduler")
            {
                fleetManager.Missions[0].saveToMemory(fleetResponseTask.Result);
            }

            logger(AREA, DEBUG, "==== Completed Get Request ====");

            return functionStatus;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveFleetStatusAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveStatus(await httpResponseTasks[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveFleetRegistersAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveRegisters(await httpResponseTasks[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveSoftwareLogsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveSoftwareLogs(await httpResponseTasks[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveSettingsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveSettings(await httpResponseTasks[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveMapsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveMaps(await httpResponseTasks[i]);
                //robots[i].saveMapsData();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveMissionsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveMissions(await httpResponseTasks[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async void pollRobots()
        {
            logger(AREA, INFO, "Harvesting Data");

            try
            {
                try
                {
                    mirFleet.issueGetRequests("status");
                    await mirFleet.saveFleetStatusAsync();

                    //mirFleet.issueGetRequests("registers");
                    //await mirFleet.saveFleetRegistersAsync();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }
        }

        /// <summary>
        /// Force the wait so we're doing things in order. Synchronous initial fetch, so there'll be some delay on start-up
        /// </summary>
        public void getInitialFleetData()
        {
            int waitPeriod = 200; // So we don't DDOS our own robots

            try
            {
                try
                {
                    mirFleet.issueGetRequests("/software/logs");
                    mirFleet.saveSoftwareLogsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            Thread.Sleep(waitPeriod);

            try
            {
                try
                {
                    mirFleet.issueGetRequests("maps");
                    mirFleet.saveMapsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            Thread.Sleep(waitPeriod);

            try
            {
                try
                {
                    mirFleet.issueGetRequests("settings");
                    mirFleet.saveSettingsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            Thread.Sleep(waitPeriod);

            try
            {
                try
                {
                    mirFleet.issueGetRequests("settings/advanced");
                    mirFleet.saveSettingsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            Thread.Sleep(waitPeriod);

            try
            {
                try
                {
                    mirFleet.issueGetRequests("missions");
                    mirFleet.saveMissionsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            Thread.Sleep(waitPeriod);
        }
    }
}