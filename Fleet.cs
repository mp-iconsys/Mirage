using System.Net.Http;
using System.Threading.Tasks;

namespace Mirage
{
    class Fleet
    {
        private Robot[] robots;
        private Task<HttpResponseMessage>[] httpResponseTasks;

        public Fleet() 
        {
            robots = new Robot[Globals.numberOfRobots];
            httpResponseTasks = new Task<HttpResponseMessage>[Globals.numberOfRobots];

            instantiateRobots(Globals.numberOfRobots);
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
            for(int i = 0; i < Globals.numberOfRobots; i++)
            {
                httpResponseTasks[i] = robots[i].sendGetRequest(type);
            }
        }

        public async Task saveFleetStatusAsync()
        {
            for (int i = 0; i < Globals.numberOfRobots; i++)
            {
                robots[i].saveStatus(await httpResponseTasks[i]);
            }
        }
    }
}
