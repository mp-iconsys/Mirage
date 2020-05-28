using System;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Mirage
{
    class Program
    {
        static int debugLevel, pollInterval, storeInterval, numberOfRobots;
        static string logFile, emailAlert, baseURL, apiUsername, apiPassword;

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

        // Authentication String
        //var authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")));

        // By declaring a single shared static we reduce the number of sockets
        // You'll need to do this per each robot -> part of the robot class???
        private static HttpClient Client = new HttpClient();

        public static async Task Main(string[] args)
        {
            ReadAllSettings();

            AuthenticationHeaderValue authValue = fetchAuthentication();

            //============================================= 
            // Default HttpClient Connection Details
            //============================================= 
            Client.BaseAddress = new Uri(baseURL);

            Client.DefaultRequestVersion = HttpVersion.Version11;

            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Add("Accept-Language", "en_US");
            Client.DefaultRequestHeaders.Authorization = authValue;
            string urlParameters = "registers/1";

            Client.Timeout = TimeSpan.FromMinutes(10);

            if (debugLevel > 0)
                Console.WriteLine("==== Starting connections ====");

            for (int i = 0; i < 2; i++)
            {
                try
                {
                    var result = await Client.GetAsync(urlParameters);

                    if (result.IsSuccessStatusCode)
                    {
                        Console.WriteLine(result.StatusCode);
                        Console.WriteLine(result.Content.ReadAsStringAsync().Result);

                        // Take the returned data and save in DB
                        urlParameters = "registers/";

                        SaveCustomObjectToDB();
                        // Decode Here
                        // Save
                    }
                    else
                    {
                        // Print error here
                    }
                }
                catch (WebException e)
                {
                    Console.WriteLine($"Connection Problems: '{e}'");
                }
            }

            if (debugLevel > 0)
                Console.WriteLine("==== Connections done ====");

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


            //Make any other calls using HttpClient here.

            //Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
            Client.Dispose();

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

        static void ReadAllSettings()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;

                if (appSettings.Count == 0)
                {
                    Console.WriteLine("AppSettings is empty.");
                }
                else
                {
                    // Print settings to make sure we're good
                    foreach (var key in appSettings.AllKeys)
                    {
                        Console.WriteLine("Key: {0} Value: {1}", key, appSettings[key]);
                    }

                    
                    debugLevel      = int.Parse(ConfigurationManager.AppSettings["debugLevel"]);
                    pollInterval    = int.Parse(ConfigurationManager.AppSettings["pollInterval"]);
                    storeInterval   = int.Parse(ConfigurationManager.AppSettings["storeInterval"]);
                    numberOfRobots  = int.Parse(ConfigurationManager.AppSettings["numberOfRobots"]);
                    logFile         = ConfigurationManager.AppSettings["logFile"];
                    emailAlert      = ConfigurationManager.AppSettings["emailAlert"];
                    baseURL         = ConfigurationManager.AppSettings["baseURL"];
                }
            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error reading app settings");
            }
        }

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

        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        static void SaveCustomObjectToDB()
        {
            // Decode object and save to DB
        }
    }
}

