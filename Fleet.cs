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
                httpResponseTasks[i] = robots[i].sendGetRequest(type);
            }
        }

        public async Task initialFleetSetUp()
        {
            issueGetRequests("/software/logs");

            await saveSoftwareLogsAsync();

            issueGetRequests("maps");

            await saveMapsAsync();

            issueGetRequests("settings");

            await saveSettingsAsync();

            issueGetRequests("settings/advanced");

            await saveSettingsAsync();

            // Download missions
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
