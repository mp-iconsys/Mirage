using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mirage
{
    class Fleet
    {
        private Robot[] robots; // TODO: Make this a list so we can add and remove Robots on demand
        private Task<HttpResponseMessage>[] httpResponseTasks; // TODO: ditto as above

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
