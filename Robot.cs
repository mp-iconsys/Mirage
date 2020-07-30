using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using Mirage.rest;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage
{
    /* Robot Class, mainly a data container
     * Contains:
     * - Connection details: IP + Authentication String
     * - Generic data about the status of the robot
     * - Registers (100 int, 100 float)
     * - Other data?
    */

    // TODO: Clean-up so we're a bit more tidy
    public class Robot
    {
        private int id = 0;
        private string ipAddress; // TODO: change to actual IPAddress class from .net library
        private AuthenticationHeaderValue authValue;

        //=========================================================|
        //  Data which makes up the robot                          |     
        //=========================================================|
        private List<Register> Registers { get; set; }
        private List<SoftwareLog> SoftwareLogs { get; set; }
        private List<Map> Maps { get; set; }
        private List<Setting> Settings { get; set; }
        public List<Mission> Missions { get; set; }
        public rest.Status s { get; set; }

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Robot);

        /// <summary>
        /// Instantiate with connection details
        /// </summary>
        public Robot()
        {
            fetchConnectionDetails();

            Registers = new List<Register>(new Register[200]);
            s = new rest.Status();
        }

        /// <summary>
        /// Instantiate with connection details
        /// </summary>
        /// <param name="id"></param>
        public Robot(int id)
        {
            this.id = id;

            fetchConnectionDetails();

            Registers = new List<Register>(new Register[200]);
            s = new rest.Status();
        }

        /// <summary>
        /// For when we're fetching the details from the database
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="authValue"></param>
        public Robot(string ipAddress, AuthenticationHeaderValue authValue)
        {
            this.ipAddress = ipAddress;
            this.authValue = authValue;

            Registers = new List<Register>(new Register[200]);
            s = new rest.Status();
        }

        /// <summary>
        /// 
        /// </summary>
        public void fetchConnectionDetails()
        {
            string apiUsername, apiPassword;

            if (Globals.resumingSession)
            {
                // We're resuming an existing session so fetch the robot connection details from a database
                string query = "SELECT IP, AUTH FROM robot WHERE ROBOT_ID =" + id;
                var getRobotData = new MySqlCommand(query, Globals.db);

                using (MySqlDataReader reader = getRobotData.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ipAddress = reader.GetString("IP");
                        authValue = new AuthenticationHeaderValue("Basic", reader.GetString("AUTH"));
                    }
                } 
            }
            else
            {
                // We've got a new session so input the details manually in the terminal
                // Firstm fetch the details

                Console.WriteLine("Please Enter The IP Address Of The Robot No " + id + ":");
                ipAddress = Console.ReadLine();
                // TODO: Check that the input is correct - length & type

                Console.WriteLine("Enter API Username:");
                apiUsername = Console.ReadLine();

                Console.WriteLine("Enter API Password:");
                apiPassword = Console.ReadLine();

                // Basic Auth type for the API. Set up as follows: BASE64( username: sha256(pass) )
                // So, first get sha256 of the pass, Concat to "username:" and then do base64 conversion
                authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiUsername}:{ComputeSha256Hash(apiPassword)}")));

                if (Globals.debugLevel > 1)
                {
                    Console.WriteLine(authValue);
                }

                // Store the data in the DB
                //string query = "REPLACE INTO robot (`ROBOT_ID`, `IP`, `AUTH`) VALUES ('" + id + "', '" + ipAddress + "', '" + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiUsername}:{ComputeSha256Hash(apiPassword)}")) + "');";
                //Globals.issueInsertQuery(query);

                // Change the App.config setting so that we load an existing config next time
                //Globals.AddUpdateAppSettings("resumingSession", "true");
            }
        }

        /// <summary>
        /// Private cause we're only using it to get the Hash
        /// Within the Robot class. Should really salt it if we're 
        /// Storing it within a DB
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        private string ComputeSha256Hash(string rawData)
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

        /// <summary>
        /// Fetch base uri used for.
        /// </summary>
        /// <returns></returns>
        public string getBaseURI()
        {
            return "http://" + ipAddress + "/api/v2.0.0/";
        }

        /// <summary>
        /// Forms a connection with a robot.
        /// </summary>
        public void formConnection()
        {
            //comms.BaseAddress = new Uri("http://" + ipAddress + "/api/v2.0.0/"); -> hhtpClient is a singleton so we can only set the defaults once
            Globals.comms.DefaultRequestHeaders.Authorization = authValue; // This might cause problems if we're using many robots with different auth strings
        }

        /// <summary>
        /// This sends an async API get request to the robot to fetch data at the specified uri
        /// It does not return data straight away. This allows us to make a bunch of calls
        /// For all of the robots and then wait for the data to get to us as it comes through.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> sendGetRequest(string uri)
        {
            formConnection();
            return await Globals.comms.GetAsync(getBaseURI() + uri);
        }

        /// <summary>
        /// Send a REST Request, either Post, Put or DELETE
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public int sendRESTdata(HttpRequestMessage request)
        {
            int statusCode = 0;

            HttpResponseMessage result = Globals.comms.SendAsync(request).Result;

            if (result.IsSuccessStatusCode)
            {
                statusCode = (int)result.StatusCode;

                if (statusCode > 199 && statusCode < 400)
                {
                    Console.WriteLine("Data Sent Successfully");
                    statusCode = Globals.Status.CompletedNoErrors;
                }
                else if (statusCode > 399)
                {
                    Console.WriteLine("Data send did not succeed");
                    statusCode = Globals.Status.CouldntProcessRequest;
                }
                else
                {
                    Console.WriteLine("Unknown Error");
                    statusCode = Globals.Status.FatalError;
                }
            }

            return statusCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveSoftwareLogs(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Software Logs ====");
            //logger(AREA, DEBUG, response.Content.ReadAsStringAsync().Result);

            SoftwareLogs = JsonConvert.DeserializeObject<List<SoftwareLog>>(response.Content.ReadAsStringAsync().Result);

            for (int i = 0; i < SoftwareLogs.Count; i++)
            {
                SoftwareLogs[i].saveToDB(id);
            }

            logger(AREA, DEBUG, "==== Finished Saving Logs ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveMaps(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Maps ====");
            //logger(AREA, DEBUG, response.Content.ReadAsStringAsync().Result);

            Maps = JsonConvert.DeserializeObject<List<Map>>(response.Content.ReadAsStringAsync().Result);

            Task<HttpResponseMessage> responseMsg;

            for (int i = 0; i < Maps.Count; i++)
            {
                responseMsg = sendGetRequest("maps/" + Maps[i].Guid);
                responseMsg.Wait(); // Block the current thread 
                                    // We want the set-up to be synchronous

                Console.WriteLine("==== Iterator : " + i + " ====");
                Console.WriteLine("==== Maps ID Prior To call: " + Maps[i].Map_id + " ====");

                Maps[i] = JsonConvert.DeserializeObject<Map>(responseMsg.Result.Content.ReadAsStringAsync().Result);
                Maps[i].Map_id = i;
                //Maps[i].print();
                Maps[i].saveToDB(id);
            }

            logger(AREA, DEBUG, "==== Finished Saving Maps ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveSettings(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Robot Settings Logs ====");

            Settings = JsonConvert.DeserializeObject<List<Setting>>(response.Content.ReadAsStringAsync().Result);

            for (int i = 0; i < Settings.Count; i++)
            {
                //Settings[i].print();
                Settings[i].saveToDB(id);
            }

            logger(AREA, DEBUG, "==== Finished Saving Logs ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveStatus(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Status Data ====");
            logger(AREA, DEBUG, response.Content.ReadAsStringAsync().Result);

            try
            {
                s = JsonConvert.DeserializeObject<rest.Status>(response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
            }

            s.print();
            s.saveToDB(id);

            logger(AREA, DEBUG, "==== Finished Saving Status ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveMissions(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Missions ====");

            Missions = JsonConvert.DeserializeObject<List<Mission>>(response.Content.ReadAsStringAsync().Result);
            logger(AREA, DEBUG, response.Content.ReadAsStringAsync().Result);

            for (int i = 0; i < Missions.Count; i++)
            {
                //Missions[i].print();
                Missions[i].saveToDB(id);
            }

            logger(AREA, DEBUG, "==== Finished Saving Missions ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveStatusInMemory(HttpResponseMessage response)
        {
            s = JsonConvert.DeserializeObject<rest.Status>(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 2)
                Globals.logJSON(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 2)
                s.print();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveRegisters(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Registers ====");

            Registers = JsonConvert.DeserializeObject<List<Register>>(response.Content.ReadAsStringAsync().Result);

            for(int i = 0;  i < Registers.Count; i++)
            { 
                //Registers[i].print();
                Registers[i].saveToDB(id);
            }

            logger(AREA, DEBUG, "==== Finished Saving Registers ====");
        }
    }
}
