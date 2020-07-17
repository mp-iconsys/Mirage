using System;
using System.Net.Http;
using System.Threading.Tasks;
using static Globals;

namespace Mirage
{
    class Fleet
    {
        public Robot[] robots; // TODO: Make this a list so we can add and remove Robots on demand
        private Task<HttpResponseMessage>[] httpResponseTasks; // TODO: ditto as above

        // Fleet manager is essentially a robot
        // keep it separate from the robot array for cleaner code
        private Task<HttpResponseMessage> fleetResponseTask;
        public Robot fleetManager;  

        public Fleet() 
        {
            robots = new Robot[Globals.sizeOfFleet];
            httpResponseTasks = new Task<HttpResponseMessage>[Globals.sizeOfFleet];

            instantiateRobots(Globals.sizeOfFleet);
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
            for(int i = 0; i < Globals.sizeOfFleet; i++)
            {
                try
                {
                    try
                    {
                        httpResponseTasks[i] = robots[i].sendGetRequest(type);
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
                catch (System.Net.WebException e)
                {
                    Console.WriteLine($"Connection Problems: '{e}'");
                }
            }
        }

        // Sync method that issues a get request and saves data in memory
        public int issueGetRequest(string type, int robotID)
        {
            int functionStatus = Status.CompletedNoErrors;

            try
            {
                try
                {
                    if(type == "mission_scheduler" || robotID == 666)
                    {
                        fleetResponseTask = fleetManager.sendGetRequest(type);
                    }
                    else
                    {
                        httpResponseTasks[robotID] = robots[robotID].sendGetRequest(type);
                    }
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

                    functionStatus = Status.CouldntProcessRequest;
                }
            }
            catch (System.Net.WebException e)
            {
                Console.WriteLine($"Connection Problems: '{e}'");

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
                }
            }
            catch (Exception e)
            {
                functionStatus = Status.CouldntProcessRequest;
            }

            if (type == "status")
            {
                robots[robotID].saveStatusInMemory(httpResponseTasks[robotID].Result);
            }
            else if (type == "mission_scheduler")
            {
                fleetManager.m.saveToMemory(fleetResponseTask.Result);
            }

            return functionStatus;
        }

        public async Task saveFleetStatusAsync()
        {
            for (int i = 0; i < Globals.sizeOfFleet; i++)
            {
                robots[i].saveStatus(await httpResponseTasks[i]);
            }
        }

        public async Task saveFleetRegistersAsync()
        {
            for (int i = 0; i < Globals.sizeOfFleet; i++)
            {
                robots[i].saveRegisters(await httpResponseTasks[i]);
            }
        }

        public async Task saveSoftwareLogsAsync()
        {
            for (int i = 0; i < Globals.sizeOfFleet; i++)
            {
                robots[i].saveSoftwareLogs(await httpResponseTasks[i]);
            }
        }

        public async Task saveSettingsAsync()
        {
            for (int i = 0; i < Globals.sizeOfFleet; i++)
            {
                robots[i].saveSettings(await httpResponseTasks[i]);
            }
        }

        public async Task saveMapsAsync()
        {
            for (int i = 0; i < Globals.sizeOfFleet; i++)
            {
                robots[i].saveMaps(await httpResponseTasks[i]);
                robots[i].saveMapsData();
            }
        }
    
    }
}
