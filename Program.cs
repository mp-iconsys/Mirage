using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using MySql.Data.MySqlClient;

namespace Mirage
{
    class Program
    {
        //============================================= 
        //  Get Static Information
        //=============================================
        // Use a form or an initial installation to get static info on first startup. Then save in config.
        // Make sure to cast the data in correct format as it's all returned as strings!!! (much pain was had by Mikolaj)
        // This includes:
        // - Test to see if we can connect to DB?
        // - Details of each of the robot: Mainly IP, etc
        // - IP of the API box
        //
        // Save these in a config file

        // By declaring a single shared static we reduce the number of sockets
        // You'll need to do this per each robot -> part of the robot class???
        //private static HttpClient Client = new HttpClient();

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

            // Download maps

            // Download missions

            // Create the fleet which will contain out robot data
            Fleet mirFleet = new Fleet();

            // Load robot data from DB if we've already configured a session

            // Create a fleet of robots which we're going to poll for data
            //Robot[] fleet = new Robot[Globals.numberOfRobots];



            //for(int i = 0; i < numberOfRobots; i++)
            //{
                // Go through the fleet
                // For each robot start a connection and issue async method to get data
                // For each of the robots issue await method to store data
                // Save to DB
                // Rinse and repeat in a while loop
            //}    

            //Robot testMRI = new Robot();

            //AuthenticationHeaderValue authValue = fetchAuthentication();

            // Works: IDs go all the way up to 200
            //Registers r = new Registers();

            /*============================================= 
            /* Default HttpClient Connection Details
            /*============================================= 
            Client.BaseAddress = new Uri(baseURL);
            Client.DefaultRequestVersion = HttpVersion.Version11;
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Add("Accept-Language", "en_US");
            Client.DefaultRequestHeaders.Authorization = authValue;
            Client.Timeout = TimeSpan.FromMinutes(10);
            ============================================= */

            if (Globals.debugLevel > 0)
                Console.WriteLine("==== Starting connections ====");

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
                    //testMRI.formConnection(comms);

                    try 
                    {
                        // We're sending GET requests to the MiR servers
                        // 
                        mirFleet.issueGetRequests("status");

                        await mirFleet.saveFleetStatusAsync();

                        // Create a task to fetch a message
                        //Task<HttpResponseMessage> m = testMRI.sendGetRequest("status");

                        // Save status 
                        //testMRI.saveStatus(await m);
                        /*
                        var result = await comms.GetAsync(urlParameters);
                        Console.WriteLine(result.Content.ReadAsStringAsync().Result);
                        //testRobot.
                        Status stat = new Status();
                        stat = JsonConvert.DeserializeObject<Status>(result.Content.ReadAsStringAsync().Result);

                        if (debugLevel > 0)
                        {
                            logJSON(result.Content.ReadAsStringAsync().Result);
                        }

                        stat.printStatus();
                        */
                    }
                    catch (HttpRequestException e)
                    {
                        // Remove the task which is causing the exception

                        Console.WriteLine("Couldn't connect to the robot");
                        Console.WriteLine("Check your network, dns settings, robot is up, etc.");
                        Console.WriteLine("Please see error log (enter location here) for more details");
                        // Store the detailed error in the error log
                        //Console.WriteLine(e);
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

        static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;
                if (settings[key] == null)
                {
                    settings.Add(key, value);
                }
                else
                {
                    settings[key].Value = value;
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
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

