using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Linq;

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
    // TODO: Define an interface
    public class Robot
    {
        private int id = 0;
        private string ipAddress; // TODO: change to actual IPAddress class from .net library
        private AuthenticationHeaderValue authValue;
        private Register[] registers;
        public List<SoftwareLog> SoftwareLogs { get; set; }
        public List<Map> Maps { get; set; }
        public List<Setting> Settings { get; set; }
        private Status s;

        // Instantiate with connection details
        public Robot()
        {
            fetchConnectionDetails();

            registers = new Register[200];
            for (int i = 0; i < registers.Length; i++)
                registers[i] = new Register();

            s = new Status();
        }

        // Instantiate with connection details
        public Robot(int id)
        {
            this.id = id;

            fetchConnectionDetails();

            registers = new Register[200];
            for (int i = 0; i < registers.Length; i++)
                registers[i] = new Register();

            s = new Status();
        }

        // For when we're fetching the details from the database
        public Robot(string ipAddress, AuthenticationHeaderValue authValue)
        {
            this.ipAddress = ipAddress;
            this.authValue = authValue;

            registers = new Register[200];
            for (int i = 0; i < registers.Length; i++)
                registers[i] = new Register();

            s = new Status();
        }

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
                string query = "INSERT INTO robot (`ROBOT_ID`, `IP`, `AUTH`) VALUES ('" + id + "', '" + ipAddress + "', '" + Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiUsername}:{ComputeSha256Hash(apiPassword)}")) + "');";
                Globals.issueInsertQuery(query);

                // Change the App.config setting so that we load an existing config next time
                Globals.AddUpdateAppSettings("resumingSession", "true");
            }
        }

        // Private cause we're only using it to get the Hash
        // Within the Robot class. Should really salt it if we're 
        // Storing it within a DB
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

        public void formConnection()
        {
            //comms.BaseAddress = new Uri("http://" + ipAddress + "/api/v2.0.0/"); -> hhtpClient is a singleton so we can only set the defaults once
            Globals.comms.DefaultRequestHeaders.Authorization = authValue; // This might cause problems if we're using many robots with different auth strings
        }

        // This sends an async API get request to the robot to fetch data at the specified uri
        // It does not return data straight away. This allows us to make a bunch of calls
        // For all of the robots and then wait for the data to get to us as it comes through.
        public async Task<HttpResponseMessage> sendGetRequest(string uri)
        {
            formConnection();
            return await Globals.comms.GetAsync(getBaseURI() + uri);
        }

        public void saveStatus(HttpResponseMessage response)
        {
            s = JsonConvert.DeserializeObject<Status>(response.Content.ReadAsStringAsync().Result);

            if(Globals.debugLevel > 2)
                Globals.logJSON(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 2)
                s.printStatus();

            s.saveStatusToDB(id, Maps);
        }

        public void saveRegisters(HttpResponseMessage response)
        {
            registers = JsonConvert.DeserializeObject<Register[]>(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 2)
            { 
                Globals.logJSON(response.Content.ReadAsStringAsync().Result);

                for(int i = 0;  i < registers.Length; i++)
                    registers[i].toString();
            }

            saveRegistersToDB(id, registers);
        }

        public void saveSoftwareLogs(HttpResponseMessage response)
        {
            if(Globals.debugLevel > 2)
                Console.WriteLine(response.Content.ReadAsStringAsync().Result);

            SoftwareLogs = JsonConvert.DeserializeObject<List<SoftwareLog>>(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 2)
            {
                Globals.logJSON(response.Content.ReadAsStringAsync().Result);

                for (int i = 0; i < SoftwareLogs.Count; i++)
                    SoftwareLogs[i].toString();
            }

            saveSoftLogsToDB(id);
        }

        public string saveRegisterToDB(Register r, int robotID)
        {
            string query;
            query = "('" + robotID + "','" + r.id + "','" + r.value + "'),";
            return query;
        }

        public void saveRegistersToDB(int robotID, Register[] registers)
        {
            string query = "INSERT INTO registers (`ROBOT_ID`, `REGISTER_ID`, `VALUE`) VALUES ";

            for (int i = 0; i < (registers.Length - 1); i++)
            {
                query += saveRegisterToDB(registers[i], robotID);
            }

            // Need to treat the last register separately
            query += "('" + robotID + "','" + registers[(registers.Length - 1)].id + "','" + registers[(registers.Length - 1)].value + "');";

            Globals.issueInsertQuery(query);
        }

        public void saveSoftLogsToDB(int robotID)
        {
            string query = "REPLACE INTO software_logs (`ROBOT_ID`, `FROM`, `TO`, `ACTION`, `STATE`, `START_TIME`, `END_TIME`, `URL`, `GUID`) VALUES ";

            for (int i = 0; i < (SoftwareLogs.Count - 1); i++)
            {
                query += "('" + robotID + "', '" + SoftwareLogs[i].From + "', '" + SoftwareLogs[i].To + "', '" + SoftwareLogs[i].Action + "', '" + SoftwareLogs[i].State + "', '" + SoftwareLogs[i].Start_time
                      + "', '" + SoftwareLogs[i].End_time + "', '" + SoftwareLogs[i].Url + "', '" + SoftwareLogs[i].Guid + "'),";
            }

            // Need to treat the last register separately
            query   += "('" + robotID + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].From + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].To + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].Action + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].State + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].Start_time
                    + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].End_time + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].Url + "', '" + SoftwareLogs[(SoftwareLogs.Count - 1)].Guid + "')";

            Globals.issueInsertQuery(query);
        }

        public void saveMaps(HttpResponseMessage response)
        {
            Maps = JsonConvert.DeserializeObject<List<Map>>(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 2)
                Globals.logJSON(response.Content.ReadAsStringAsync().Result);

                for(int i = 0; i < Maps.Count; i++)
                {
                    //Console.WriteLine("Iterating : " + i);

                    Maps[i].Map_id = i;

                    if (Globals.debugLevel > 1)
                        Maps[i].printMap();
                }    
        }

        public void saveMapsData()
        {
            Task<HttpResponseMessage> responseMsg;

            for (int i = 0; i < Maps.Count; i++)
            {
                responseMsg = sendGetRequest("maps/" + Maps[i].Guid);
                responseMsg.Wait(); // Block the current thread 
                                    // We want the set-up to be synchronous

                if(Globals.debugLevel > 3)
                { 
                    Console.WriteLine("==== Iterator : " + i + " ====");
                    Console.WriteLine("==== Maps ID Prior To call: " + Maps[i].Map_id + " ====");
                }

                Maps[i] = JsonConvert.DeserializeObject<Map>(responseMsg.Result.Content.ReadAsStringAsync().Result);
                Maps[i].Map_id = i;
                //Maps[i].printMap();
            }

            if (Globals.debugLevel > 1)
            {
                for (int i = 0; i < Maps.Count; i++)
                    Maps[i].printMap();
            }

            saveMapsToDB();
        }

        public void saveMapsToDB()
        {
            string query = "REPLACE INTO maps (`MAP_ID`, `ROBOT_ID`, `NAME`, `CREATED_BY_NAME`, `CREATED_BY_ID`, `MAP`, `METADATA`, `ONE_WAY_MAP`, `ORIGIN_THETA`, `ORIGIN_X`, `ORIGIN_Y`, `PATH_GUIDES`, `PATHS`, `POSITIONS`, `RESOLUTION`) VALUES ";

            int i;

            for (i = 0; i < (Maps.Count - 1); i++)
            {
                query   += "('" + Maps[i].Map_id + "', '" + id + "', '" + Maps[i].Name + "', '" + Maps[i].Created_by_name + "', '" + Maps[i].Created_by_id + "', '" + Maps[i].map + "', '" + Maps[i].Metadata + "', '"
                        + Maps[i].One_way_map + "', '" + Maps[i].Origin_theta + "', '" + Maps[i].Origin_x + "', '" + Maps[i].Origin_y + "', '" + Maps[i].Path_guides + "', '" + Maps[i].Paths + "', '"
                        + Maps[i].Positions + "', '" + Maps[i].Resolution + "'),";
            }

            // Need to treat the last register separately
            i = (Maps.Count - 1);

            query += "('" + Maps[i].Map_id + "', '" + id + "', '" + Maps[i].Name + "', '" + Maps[i].Created_by_name + "', '" + Maps[i].Created_by_id + "', '" + Maps[i].map + "', '" + Maps[i].Metadata + "', '"
                    + Maps[i].One_way_map + "', '" + Maps[i].Origin_theta + "', '" + Maps[i].Origin_x + "', '" + Maps[i].Origin_y + "', '" + Maps[i].Path_guides + "', '" + Maps[i].Paths + "', '"
                    + Maps[i].Positions + "', '" + Maps[i].Resolution + "');";

            Globals.issueInsertQuery(query);
        }

        public string getBaseURI()
        {
            return "http://" + ipAddress + "/api/v2.0.0/";
        }

        public void saveSettings(HttpResponseMessage response)
        {
            Settings = JsonConvert.DeserializeObject<List<Setting>>(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 3)
            {
                Globals.logJSON(response.Content.ReadAsStringAsync().Result);

                for (int i = 0; i < Settings.Count; i++)
                    Settings[i].toString();
            }

            saveSettingsToDB();
        }

        public void saveSettingsToDB()
        {
            string query = "REPLACE INTO settings (`SETTING_ID`, `ROBOT_ID`, `NAME`, `PARENT_NAME`, `URL`, `VALUE`, `DEFAULT_VALUE`) VALUES ";

            int i;

            for (i = 0; i < (Settings.Count - 1); i++)
            {
                query += "('" + Settings[i].Id + "','" + id + "','" + Settings[i].Name + "','" + Settings[i].Parent_name + "','" + Settings[i].Url + "','" + Settings[i].Value + "','" + Settings[i].Default + "'),";
            }

            // Need to treat the last register separately
            i = (Settings.Count - 1);
            query += "('" + Settings[i].Id + "','" + id + "','" + Settings[i].Name + "','" + Settings[i].Parent_name + "','" + Settings[i].Url + "','" + Settings[i].Value + "','" + Settings[i].Default + "');";

            Globals.issueInsertQuery(query);
        }

        public class Register
        {
            public int id { get; set; }
            public string label { get; set; }
            public string url { get; set; }
            public float value { get; set; }

            public Register() {}

            public string getURL()
            {
                return url + id;
            }

            public void toString()
            {
                Console.WriteLine("id: " + id + " Label: " + label + " url: " + url + " getURL: " + getURL() + " value: " + value);
            }
        }

        public class SoftwareLog
        {
            public string Action { get; set; }
            public string End_time { get; set; }
            public string From { get; set; }
            public string Guid { get; set; }
            public string Start_time { get; set; }
            public string State { get; set; }
            public string To { get; set; }
            public string Url { get; set; }

            public void toString()
            {
                Console.WriteLine("Action: " + Action);
                Console.WriteLine("End_time: " + End_time);
                Console.WriteLine("From: " + From);
                Console.WriteLine("Guid: " + Guid);
                Console.WriteLine("Start_time: " + Start_time);
                Console.WriteLine("State: " + State);
                Console.WriteLine("To: " + To);
                Console.WriteLine("Url: " + Url);
            }
        }

        public class Status
        {
            public int mode_id { get; set; }
            public int state_id { get; set; }
            public int uptime { get; set; }
            public int battery_time_remaining { get; set; } // In seconds
            public float battery_percentage { get; set; }
            public float distance_to_next_target { get; set; }
            public float moved { get; set; }
            public string allowed_methods { get; set; }
            public string footprint { get; set; }
            public string joystick_low_speed_mode_enabled { get; set; }
            public string joystick_web_session_id { get; set; }
            public string map_id { get; set; }
            public string mission_queue_id { get; set; }
            public string mission_queue_url { get; set; }
            public string mission_text { get; set; }
            public string mode_key_state { get; set; }
            public string mode_text { get; set; }
            public string robot_model { get; set; }
            public string robot_name { get; set; }
            public string safety_system_muted { get; set; }
            public string serial_number { get; set; }
            public string session_id { get; set; }
            public string state_text { get; set; }
            public string unloaded_map_changes { get; set; }

            public Position position { get; set; }
            public List<ErrorsItem> errors { get; set; } // Try without a list
            public User_prompt user_prompt { get; set; }
            public Velocity velocity { get; set; }

            public Status() { }

            public void printStatus()
            {
                Console.WriteLine("mode_id: " + mode_id);
                Console.WriteLine("state_id: " + state_id);
                Console.WriteLine("uptime: " + uptime);
                Console.WriteLine("battery_time_remaining: " + battery_time_remaining);
                Console.WriteLine("battery_percentage: " + battery_percentage);
                Console.WriteLine("distance_to_next_target: " + distance_to_next_target);
                Console.WriteLine("allowed_methods: " + allowed_methods);
                Console.WriteLine("footprint: " + footprint);
                Console.WriteLine("joystick_low_speed_mode_enabled: " + joystick_low_speed_mode_enabled);
                Console.WriteLine("joystick_web_session_id: " + joystick_web_session_id);
                Console.WriteLine("moved: " + moved);
                Console.WriteLine("velocity linear: " + velocity.linear);
                Console.WriteLine("velocity angular: " + velocity.angular);
            }

            public void saveStatusToDB(int robotID, List<Map> m)
            {
                int?    position_id = null, 
                        error_id = null, 
                        user_promt_id = null, 
                        velocity_id = null;
                string  query;

                // First, populate child tables
                if (position != null)
                {
                    query = "INSERT INTO position (`X`, `Y`, `ORIENTATION`) VALUES ('";
                    query += position.x + "', '" + position.y + "', '" + position.orientation + "');";

                    Globals.issueInsertQuery(query);

                    position_id = Globals.getIDQuery("position");
                }
                else
                {
                    // No position data
                }

                if (velocity != null)
                {
                    query = "INSERT INTO velocity (`LINEAR`, `ANGULAR`) VALUES ('";
                    query += velocity.linear + "', '" + velocity.angular + "');";

                    Globals.issueInsertQuery(query);

                    velocity_id = Globals.getIDQuery("velocity");
                }
                else
                {
                    // No position data
                }

                
                if (errors != null)
                {
                    if(errors.Count == 0 || !errors.Any())
                    {
                        if(Globals.debugLevel > 1)
                            Console.WriteLine("==== Robot " + robotID + " Has No Errors ====");
                    }
                    else
                    { 
                        query = "REPLACE INTO error (CODE, DESCRIPTION, MODULE) VALUES ";

                        int i;

                        // Assume we only have one error here - no need to faff around
                        for(i = 0; i < (errors.Count-1); i++)
                        { 
                            query += "('" + errors[i].code + "','" + MySqlHelper.EscapeString(errors[i].description) + "', '" + MySqlHelper.EscapeString(errors[i].module) + "'),";
                        }

                        i = errors.Count - 1;
                        query += "('" + errors[i].code + "','" + MySqlHelper.EscapeString(errors[i].description) + "', '" + MySqlHelper.EscapeString(errors[i].module) + "');";

                        Globals.issueInsertQuery(query);

                        error_id = errors[i].code;
                    }
                }
                else
                {
                    // No position data
                }
                
                /*
                if (user_prompt != null)
                {
                    query = "INSERT INTO position (X, Y, ORIENTATION) VALUES ('";
                    query += position.x + "', '" + position.y + "', '" + position.orientation + "');";

                    Globals.issueInsertQuery(query);

                    position_id = Globals.getIDQuery("position");
                }
                else
                {
                    // No position data
                }
                */
                // Removed following fields from the query:
                // - serial_number
                // - session_id
                // - mode_key_state
                // - mission_queue_url
                // - footprint ???

                query = "INSERT INTO robot_status (ROBOT_ID, MODE_ID, STATE_ID, UPTIME, BATTERY_TIME_REMAINING, BATTERY_PERCENTAGE, DISTANCE_TO_NEXT_TARGET, MOVED, joystick_low_speed_mode_enabled,"; // FOOTPRINT, ALLOWED_METHODS,
                query += "joystick_web_session_id, map_id, mission_queue_id, mission_text, mode_text, safety_system_muted, "; //mission_queue_url, robot_model, robot_name, state_text, mode_key_state, serial_number, session_id,
                query += "unloaded_map_changes, POSITION_ID, ERROR_CODE, USER_PROMPT_ID, VELOCITY_ID) ";

                query += "VALUES (";
                query += Globals.addToDB(robotID);
                query += Globals.addToDB(mode_id);
                query += Globals.addToDB(state_id);
                query += Globals.addToDB(uptime);
                query += Globals.addToDB(battery_time_remaining);
                query += Globals.addToDB(battery_percentage);
                query += Globals.addToDB(distance_to_next_target);
                query += Globals.addToDB(moved);
                //query += Globals.addToDB(allowed_methods);
                //query += Globals.addToDB(footprint);
                query += Globals.addToDB(joystick_low_speed_mode_enabled);
                query += Globals.addToDB(joystick_web_session_id);
                
                for(int i = 0; i < m.Count; i++)
                {
                    if (m[i].Guid == map_id)
                        map_id = m[i].Map_id.ToString();
                }
                
                query += Globals.addToDB(map_id);
                query += Globals.addToDB(mission_queue_id);
                //query += Globals.addToDB(mission_queue_url);
                try 
                { 
                    query += Globals.addToDB(MySqlHelper.EscapeString(mission_text));
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine("Oh hey, obscure MySQL helper bug.");
                    Console.WriteLine("Actual Exception: ");
                    Console.WriteLine(e);

                    query += Globals.addToDB(MySqlHelper.EscapeString("hello, this is expt"));
                }
                //query += Globals.addToDB(mode_key_state);
                query += Globals.addToDB(mode_text);
                //query += Globals.addToDB(robot_model);
                //query += Globals.addToDB(robot_name);
                query += Globals.addToDB(safety_system_muted);
                //query += Globals.addToDB(serial_number);
                //query += Globals.addToDB(session_id);
                //query += Globals.addToDB(state_text);
                query += Globals.addToDB(unloaded_map_changes);
                query += Globals.addToDB(position_id);
                query += Globals.addToDB(error_id);
                query += Globals.addToDB(user_promt_id);
                query += "'" + velocity_id + "');";

                Globals.issueInsertQuery(query);
            }

            /*  Nested Classes, to contain data in a nicer way
             * 
             * 
            */
            public class Position
            {
                public double x { get; set; }
                public double y { get; set; }
                public double orientation { get; set; }
            }

            public class ErrorsItem
            {
                public int code { get; set; }
                public string description { get; set; }
                public string module { get; set; }
            }

            public class Velocity
            {
                public double linear { get; set; }
                public double angular { get; set; }
            }

            public class User_prompt
            {
                public string guid { get; set; }
                public string question { get; set; }
                public List<string> options { get; set; }
                public int timeout { get; set; }
                public string user_group { get; set; }
            }

            public class Cart
            {
                public int id { get; set; }
                public int length { get; set; }
                public int width { get; set; }
                public int height { get; set; }
                public int offset_locked_wheels { get; set; }
            }

            public class Hook_status
            {
                public string available { get; set; }
                public int angle { get; set; }
                public Cart cart { get; set; }
                public int height { get; set; }
                public string braked { get; set; }
                public int length { get; set; }
                public string cart_attached { get; set; }
            }
        }

        public class Map
        {
            public List<string> Allowed_methods { get; set; }
            public string Created_by { get; set; }
            public string Created_by_id { get; set; }
            public string Created_by_name { get; set; }
            public string Guid { get; set; }
            public string map { get; set; }
            public string Metadata { get; set; }
            public string Name { get; set; }
            public string One_way_map { get; set; }
            public float Origin_theta { get; set; }
            public float Origin_x { get; set; }
            public float Origin_y { get; set; }
            public string Path_guides { get; set; }
            public string Paths { get; set; }
            public string Positions { get; set; }
            public double Resolution { get; set; }
            public string Session_id { get; set; }
            public string Url { get; set; }
            public int Map_id { get; set; }

            public void printMap()
            {
                Console.WriteLine();
                Console.WriteLine("==== MAP NO: " + Map_id + " ====");
                Console.WriteLine("Created_by: " + Created_by);
                Console.WriteLine("Created_by_id: " + Created_by_id);
                Console.WriteLine("Created_by_name: " + Created_by_name);
                Console.WriteLine("Guid: " + Guid);
                Console.WriteLine("map: " + map);
                Console.WriteLine("Metadata: " + Metadata);
                Console.WriteLine("Name: " + Name);
                Console.WriteLine("One_way_map: " + One_way_map);
                Console.WriteLine("Origin_theta: " + Origin_theta);
                Console.WriteLine("Origin_x: " + Origin_x);
                Console.WriteLine("Origin_y: " + Origin_y);
                Console.WriteLine("Path_guides: " + Path_guides);
                Console.WriteLine("Paths: " + Paths);
                Console.WriteLine("Positions: " + Positions);
                Console.WriteLine("Resolution: " + Resolution);
                Console.WriteLine("Session_id: " + Session_id);
                Console.WriteLine("Url: " + Url);
                Console.WriteLine("==== END OF MAP PRINT NO: " + Map_id + " ====");
                Console.WriteLine();
            }
        }
    
        public class Setting
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Parent_name { get; set; }
            public string Url { get; set; }
            public string Value { get; set; }
            public string Default { get; set; }

            public void toString()
            {
                Console.WriteLine("ID: " + Id);
                Console.WriteLine("Name: " + Name);
                Console.WriteLine("Parent_name: " + Parent_name);
                Console.WriteLine("Url: " + Url);
                Console.WriteLine("Value: " + Value);
                Console.WriteLine("Default: " + Default);
            }
        }
    }
}
