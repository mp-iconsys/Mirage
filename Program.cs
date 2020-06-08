using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace Mirage
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Capture CTRL+C
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) 
            {
                e.Cancel = true;
                Globals.keepRunning = false;
            };

            // TODO: Capture seg faults 

            // TODO: some sort of thread diagnostics???

            Globals.readAllSettings();

            Globals.connectToDB();

            Globals.setUpDefaultComms();

            // Create the fleet which will contain out robot data
            Fleet mirFleet = new Fleet();

            mirFleet.issueGetRequests("/software/logs");

            await mirFleet.saveSoftwareLogsAsync();

            mirFleet.issueGetRequests("maps");

            await mirFleet.saveMapsAsync();

            mirFleet.issueGetRequests("settings");

            await mirFleet.saveSettingsAsync();

            mirFleet.issueGetRequests("settings/advanced");

            await mirFleet.saveSettingsAsync();

            // Download missions

            // Load robot data from DB if we've already configured a session

            if (Globals.debugLevel > 0)
                Console.WriteLine("==== Starting Main Loop ====");

            //============================================= 
            // M A I N      L O O P
            //============================================= 
            int i = 0;

            while (Globals.keepRunning)
            {
                if(Globals.debugLevel > 0)
                    Console.WriteLine("==== Loop No: " + ++i + " ====");

                try
                {
                    try 
                    {
                        // We're sending GET requests to the MiR servers
                        // Saving them asynchronously as they come along

                        mirFleet.issueGetRequests("status");

                        await mirFleet.saveFleetStatusAsync();

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

                Thread.Sleep(Globals.pollInterval*1000); // Ugly as fuck but will change to event based stuff once I add a GUI
            }

            Globals.closeComms();

            Console.WriteLine("==== Graceful Exit ====");
            Environment.Exit(1);

            /*
            HttpResponseMessage response = Client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                var dataObjects = response.Content.ReadAsAsync<IEnumerable<DataObject>>().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                foreach (var d in dataObjects)
                {
                    Console.WriteLine("{0}", d.Name);
                }
            }
            else
            {
                Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }
            */


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
    }
}

