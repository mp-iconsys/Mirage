using System;
using System.Net;
using System.Net.Http;
using System.Timers;

namespace Mirage
{
    class MirageService
    {
        public static Fleet mirFleet;

        public void Start()
        {
            // write code here that runs when the Windows Service starts up.  
            Globals.readAllSettings();

            /*
            Globals.connectToDB();

            Globals.setUpDefaultComms();

            // Create the fleet which will contain out robot data
            mirFleet = new Fleet();

            mirFleet.initialFleetSetUp(); // Block the thread until we get initial data

            // Load robot data from DB if we've already configured a session

            if (Globals.debugLevel > -1)
                Console.WriteLine("==== Starting Main Loop ====");

            //============================================= 
            // M A I N      L O O P
            //============================================= 
            Timer pollingTimer = new Timer();
            pollingTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            pollingTimer.Interval = Globals.pollInterval;
            pollingTimer.Enabled = true;
            */
        }

        public void Stop()
        {
            // write code here that runs when the Windows Service stops.  
            Globals.closeComms();

            Logger.Info("==== Graceful Exit ====", "Exit");

            //Environment.Exit(1);
        }

        public void Pause()
        {

        }

        public void Continue()
        {

        }

        private async static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            Console.WriteLine("Polling @ {0}", e.SignalTime);

            try
            {
                try
                {
                    // We're sending GET requests to the MiR servers
                    // Saving them asynchronously as they come along

                    Console.WriteLine("==== Getting Status ====");

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
    }
}
