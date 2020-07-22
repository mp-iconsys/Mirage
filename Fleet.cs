using System;
using System.Net.Http;
using System.Threading.Tasks;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage
{
    class Fleet
    {
        //=========================================================|
        // Fleet manager is essentially a robot                    |
        // keep it separate from the robot array for cleaner code  |
        //=========================================================|
        // TODO : Make these all lists, so the size can be amended at runtime
        public Robot[] robots;                                      // TODO: Make this a list so we can add and remove Robots on demand
        public Robot fleetManager;
        private Task<HttpResponseMessage>[] httpResponseTasks; // TODO: ditto as above
        private Task<HttpResponseMessage> fleetResponseTask;

        //=========================================================|
        //  Used For Debugging                                     |     
        //=========================================================|
        private static readonly Type AREA = typeof(Fleet);

        public Fleet() 
        {
            robots = new Robot[sizeOfFleet];
            httpResponseTasks = new Task<HttpResponseMessage>[sizeOfFleet];

            instantiateRobots(sizeOfFleet);
        }

        public Fleet (int sizeOfFleet)
        {
            robots = new Robot[sizeOfFleet];
            httpResponseTasks = new Task<HttpResponseMessage>[sizeOfFleet];

            instantiateRobots(sizeOfFleet);
        }

        public void instantiateRobots(int sizeOfFleet)
        {
            fleetManager = new Robot(fleetManagerIP, fleetManagerAuthToken);

            for (int i = 0; i < sizeOfFleet; i++)
            {
                // Instantiate the robots - Don't touch the tasks yet
                robots[i] = new Robot(i);
            }
        }

        public void issueGetRequests(string type)
        {
            for(int i = 0; i < sizeOfFleet; i++)
            {
                try
                {
                    try
                    {
                        httpResponseTasks[i] = robots[i].sendGetRequest(type);
                    }
                    catch (HttpRequestException exception)
                    {
                        // TODO: Handle more exceptions
                        // Remove the task which is causing the exception

                        Console.WriteLine("Couldn't connect to the robot");
                        Console.WriteLine("Check your network, dns settings, robot is up, etc.");
                        Console.WriteLine("Please see error log (enter location here) for more details");
                        // Store the detailed error in the error log
                        Console.WriteLine(exception);
                    }
                }
                catch (System.Net.WebException exception)
                {
                    Console.WriteLine($"Connection Problems: '{exception}'");
                }
            }
        }

        // Sync method that issues a get request and saves data in memory
        public int issueGetRequest(string type, int robotID)
        {
            logger(AREA, DEBUG, "==== Issuing Get Request ====");

            int functionStatus = Status.CompletedNoErrors;

            try
            {
                try
                {
                    if(type == "mission_scheduler" || robotID == 666)
                    {
                        logger(AREA, DEBUG, "Sending " + type + " To Mission Scheduler");
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
                    logger(AREA, ERROR, "Couldn't Connect To The Robot. Check Your Network, DNS Settings, Robot Status, etc.");
                    logger(AREA, ERROR, "The Error Is: ", exception);

                    functionStatus = Status.CouldntProcessRequest;
                }
            }
            catch (System.Net.WebException exception)
            {
                logger(AREA, ERROR, "Connection Problems: ", exception);

                functionStatus = Status.CouldntProcessRequest;
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
                functionStatus = Status.CouldntProcessRequest;

                logger(AREA, ERROR, "Connection Problems: ", exception);
            }

            if (type == "status")
            {
                robots[robotID].saveStatusInMemory(httpResponseTasks[robotID].Result);

                logger(AREA, DEBUG, "Status is : " + robots[robotID].s.mission_text);
            }
            else if (type == "mission_scheduler")
            {
                fleetManager.m.saveToMemory(fleetResponseTask.Result);
            }

            logger(AREA, DEBUG, "==== Completed Get Request ====");

            return functionStatus;
        }

        public async Task saveFleetStatusAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveStatus(await httpResponseTasks[i]);
            }
        }

        public async Task saveFleetRegistersAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveRegisters(await httpResponseTasks[i]);
            }
        }

        public async Task saveSoftwareLogsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveSoftwareLogs(await httpResponseTasks[i]);
            }
        }

        public async Task saveSettingsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveSettings(await httpResponseTasks[i]);
            }
        }

        public async Task saveMapsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i].saveMaps(await httpResponseTasks[i]);
                robots[i].saveMapsData();
            }
        }
    
    }
}
