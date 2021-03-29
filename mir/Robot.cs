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
using System.Data;

// TODO: Clean-up so we're a bit more tidy
public class Robot
{
    public int id {get; set; } //Used to be: public int id = 0; 
    public int fleetRobotID { get; set; }
    public int plcRobotID { get; set; }
    public string ipAddress { get; set; } // TODO: change to actual IPAddress class from .net library
    private AuthenticationHeaderValue authValue;

    //=========================================================|
    //  KPI, OEM & Statistics                                  |
    //=========================================================|
    /*    public long job { get; set; }
        public List<int> MissionsForDB { get; set; }
    */
    public int maxRegister = 13;

    //=========================================================|
    //  Data which makes up the robot                          |     
    //=========================================================|
    public List<Register> Registers { get; set; }
    private List<SoftwareLog> SoftwareLogs { get; set; }
    private List<Map> Maps { get; set; }
    private List<Setting> Settings { get; set; }
    public List<Mission> Missions { get; set; }
    public Status s { get; set; }
    public Scheduler schedule { get; set; }
    public FireAlarms FireAlarm { get; set; }
    public RobotGroup Group { get; set; }
    public Job currentJob { get; set; }


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
            s = new Status();
            schedule = new Scheduler();

            fleetRobotID = 0;
            plcRobotID = 0;

            currentJob = new Job();
        }

        /// <summary>
        /// Instantiate with connection details
        /// </summary>
        /// <param name="id"></param>
        public Robot(int id)
        {
            this.id = id;
            plcRobotID = id + 1; // The PLC Uses Robot 0 as a spare while it's used for us so we need to offset by 1 

            fetchConnectionDetails();

            Registers = new List<Register>(new Register[200]);
            s = new Status();
            schedule = new Scheduler();
            currentJob = new Job();
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
            s = new Status();
            FireAlarm = new FireAlarms();
            Group = new RobotGroup();
            schedule = new Scheduler();

            Missions = new List<Mission>(new Mission[80]);
            currentJob = new Job();

            for (int i = 0; i < 80; i++)
            {
                Missions[i] = new Mission();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void fetchConnectionDetails()
        {
            string apiUsername, apiPassword;

            if (resumingSession)
            {
                // We're resuming an existing session so fetch the robot connection details from a database
                string query = "SELECT IP, AUTH FROM robot WHERE ROBOT_ID =" + id;
                var getRobotData = new MySqlCommand(query, db);

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

                logger(AREA, DEBUG, authValue.ToString());

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
            //setUpDefaultComms();

            //comms.BaseAddress = new Uri("http://" + ipAddress + "/api/v2.0.0/"); //-> hhtpClient is a singleton so we can only set the defaults once

            comms.DefaultRequestHeaders.Authorization = authValue; // This might cause problems if we're using many robots with different auth strings

            logger(AREA, DEBUG, "The IP is: " + ipAddress);
            logger(AREA, DEBUG, "Set the base address");
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
            return await comms.GetAsync(getBaseURI() + uri);
        }

        /// <summary>
        /// Send a REST Request, either Post, Put or DELETE
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public int sendRESTdata(HttpRequestMessage request)
        {
            formConnection();

            int statusCode = 0;

            try
            {
                HttpResponseMessage result = comms.SendAsync(request).Result;

                logger(AREA, DEBUG, "Status Code: " + result.StatusCode);

                if (result.IsSuccessStatusCode)
                {
                    logger(AREA, INFO, "Status Code: ");

                    statusCode = (int)result.StatusCode;

                    logger(AREA, INFO, "Status Code: ");

                    //schedule = JsonConvert.DeserializeObject<Scheduler>(result.Content.ReadAsStringAsync().Result);
                    //schedule.working_response = true;

                    if (result.StatusCode.ToString() == "BadRequest")
                    {
                        logger(AREA, DEBUG, "Bad Request - Failed to process");
                        statusCode = Globals.TaskStatus.CouldntProcessRequest;
                    }
                    else if (statusCode == 409)
                    {
                        logger(AREA, INFO, "Robot Already Moved out of the group");
                        statusCode = Globals.TaskStatus.CompletedNoErrors;
                    }
                    else if ((statusCode > 199 && statusCode < 400) || result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        logger(AREA, DEBUG, "Data Sent Successfully");
                        statusCode = Globals.TaskStatus.CompletedNoErrors;
                    }
                    else if (statusCode > 399)
                    {
                        logger(AREA, DEBUG, "Data send did not succeed");
                        statusCode = Globals.TaskStatus.CouldntProcessRequest;
                    }
                    else
                    {
                        logger(AREA, DEBUG, "Unknown Error");
                        statusCode = Globals.TaskStatus.FatalError;
                    }
                }
                else if((int)result.StatusCode == 409)
                {
                    logger(AREA, INFO, "Robot Already Moved out of the group");
                    statusCode = Globals.TaskStatus.CompletedNoErrors;
                }
                else
                {
                    logger(AREA, DEBUG, "Bad Request - Failed to process");
                    statusCode = Globals.TaskStatus.CouldntProcessRequest;

                    //schedule.working_response = false;
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode send REST request: ", exception);
            }

            return statusCode;
        }

    /// <summary>
    /// Sends a mission schedule, either to a robot (through fleet) or to the fleet.
    /// Returns the API status code and saves the schedule id for each robot. 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public int sendScheduleRequest(HttpRequestMessage request)
    {
        formConnection();

        int statusCode = 0;

        try
        {
            HttpResponseMessage result = comms.SendAsync(request).Result;

            logger(AREA, DEBUG, "Status Code: " + result.StatusCode);

            if (result.IsSuccessStatusCode)
            {
                logger(AREA, INFO, "Status Code: ");

                statusCode = (int)result.StatusCode;

                logger(AREA, INFO, "Status Code: ");

                schedule = JsonConvert.DeserializeObject<Scheduler>(result.Content.ReadAsStringAsync().Result);
                schedule.working_response = true;

                if (result.StatusCode.ToString() == "BadRequest")
                {
                    logger(AREA, DEBUG, "Bad Request - Failed to process");
                    statusCode = Globals.TaskStatus.CouldntProcessRequest;
                }
                else if (statusCode == 409)
                {
                    logger(AREA, INFO, "Robot Already Moved out of the group");
                    statusCode = Globals.TaskStatus.CompletedNoErrors;
                }
                else if ((statusCode > 199 && statusCode < 400) || result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    logger(AREA, DEBUG, "Data Sent Successfully");
                    statusCode = Globals.TaskStatus.CompletedNoErrors;
                }
                else if (statusCode > 399)
                {
                    logger(AREA, DEBUG, "Data send did not succeed");
                    statusCode = Globals.TaskStatus.CouldntProcessRequest;
                }
                else
                {
                    logger(AREA, DEBUG, "Unknown Error");
                    statusCode = Globals.TaskStatus.FatalError;
                }
            }
            else if ((int)result.StatusCode == 409)
            {
                logger(AREA, INFO, "Robot Already Moved out of the group");
                statusCode = Globals.TaskStatus.CompletedNoErrors;
            }
            else
            {
                logger(AREA, DEBUG, "Bad Request - Failed to process");
                statusCode = Globals.TaskStatus.CouldntProcessRequest;

                schedule.working_response = false;
            }
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "Failed to decode send REST request: ", exception);
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

            try
            {
                SoftwareLogs = JsonConvert.DeserializeObject<List<SoftwareLog>>(response.Content.ReadAsStringAsync().Result);

                for (int i = 0; i < SoftwareLogs.Count; i++)
                {
                    SoftwareLogs[i].saveToDB(id);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }

            logger(AREA, DEBUG, "==== Finished Saving Software Logs ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveRobotGroup(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Robot Group Data ====");

            try
            {
                Group = JsonConvert.DeserializeObject<RobotGroup>(response.Content.ReadAsStringAsync().Result);

                Group.print();

/*                for (int i = 0; i < SoftwareLogs.Count; i++)
                {
                    SoftwareLogs[i].saveToDB(id);
                }*/
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }

            logger(AREA, DEBUG, "==== Finished Saving Software Logs ====");
        }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    public void saveMaps(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Maps ====");

            try
            {
                Maps = JsonConvert.DeserializeObject<List<Map>>(response.Content.ReadAsStringAsync().Result);

                Task<HttpResponseMessage> responseMsg;

                for (int i = 0; i < Maps.Count; i++)
                {
                    responseMsg = sendGetRequest("maps/" + Maps[i].Guid);
                    responseMsg.Wait(); // Block the current thread as we want the set-up to be synchronous

                    logger(AREA, DEBUG, "Map No: " + i);

                    Maps[i] = JsonConvert.DeserializeObject<Map>(responseMsg.Result.Content.ReadAsStringAsync().Result);
                    Maps[i].Map_id = i;
                    //Maps[i].print();
                    Maps[i].saveToDB(id);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }

            logger(AREA, DEBUG, "==== Finished Saving Maps ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveSettings(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Robot Settings ====");

            try
            {
                Settings = JsonConvert.DeserializeObject<List<Setting>>(response.Content.ReadAsStringAsync().Result);

                for (int i = 0; i < Settings.Count; i++)
                {
                    //Settings[i].print();
                    Settings[i].saveToDB(id);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }

            logger(AREA, DEBUG, "==== Finished Saving Settings ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveStatus(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Status Data ====");

            try
            {
                s = JsonConvert.DeserializeObject<Status>(response.Content.ReadAsStringAsync().Result);

                //s.print();
                s.saveToDB(id);
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }

            logger(AREA, DEBUG, "==== Finished Saving Status ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveMissions(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Missions ====");

            try
            {
                Missions = JsonConvert.DeserializeObject<List<Mission>>(response.Content.ReadAsStringAsync().Result);

            logger(AREA, DEBUG, "no of missions is: " + Missions.Count);

                for (int i = 0; i < Missions.Count; i++)
                {
                    Missions[i].saveToDB(id, i);
                    Missions[i].print();       
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }

            logger(AREA, DEBUG, "==== Finished Saving Missions ====");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveStatusInMemory(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Status In Memory ====");

            try
            {
                s = JsonConvert.DeserializeObject<Status>(response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }
        }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    public void saveRegistersWithoutDB(HttpResponseMessage response)
    {
        logger(AREA, DEBUG, "==== Saving Registers ====");

        try
        {
            Registers = JsonConvert.DeserializeObject<List<Register>>(response.Content.ReadAsStringAsync().Result);
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
            logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
        }

        logger(AREA, DEBUG, "==== Finished Saving Registers ====");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="response"></param>
    public void saveRegisters(HttpResponseMessage response)
        {
            logger(AREA, DEBUG, "==== Saving Registers ====");

            try
            {
                Registers = JsonConvert.DeserializeObject<List<Register>>(response.Content.ReadAsStringAsync().Result);
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed to decode JSON data: ", exception);
                logger(AREA, ERROR, response.Content.ReadAsStringAsync().Result);
            }

        // Now save registers to DB (from memory)
        //logger(AREA, INFO, "Saving The Registers In The DB");

        try
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = db;
            cmd.CommandText = "store_12_registers";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@ROBOT_ID", id);
            cmd.Parameters["@ROBOT_ID"].Direction = ParameterDirection.Input;
            
            string param = "";

            for (int regNo = 0; regNo < maxRegister; regNo++)
            {
                param = "@REG" + (regNo + 1);
                cmd.Parameters.AddWithValue(param, Registers[regNo].value);
                cmd.Parameters[param].Direction = ParameterDirection.Input;
            }
/*


            cmd.Parameters.AddWithValue("@REG2", totalNoOfMissions);
            cmd.Parameters["@REG2"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@REG3", start.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters["@REG3"].Direction = ParameterDirection.Input;

            cmd.Parameters.AddWithValue("@REG4", end.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters["@REG4"].Direction = ParameterDirection.Input;*/

            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "MySQL Query Error: ", exception);
        }

        logger(AREA, DEBUG, "==== Finished Saving Registers ====");
        }
    }