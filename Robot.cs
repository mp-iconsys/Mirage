using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;

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

                Console.WriteLine("Please Enter The IP Address Of The Robot No" + id + ":");
                ipAddress = Console.ReadLine();
                // TODO: Check that the input is correct - length & type

                Console.WriteLine("Enter API Username:");
                apiUsername = Console.ReadLine();

                Console.WriteLine("Enter API Password:");
                apiPassword = Console.ReadLine();

                // Basic Auth type for the API. Set up as follows: BASE64( username: sha256(pass) )
                // So, first get sha256 of the pass, Concat to "username:" and then do base64 conversion
                authValue = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiUsername}:{ComputeSha256Hash(apiPassword)}")));

                if (Globals.debugLevel > 0)
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

            if(Globals.debugLevel > 0)
                Globals.logJSON(response.Content.ReadAsStringAsync().Result);

            if (Globals.debugLevel > 0)
                s.printStatus();

            s.saveStatusToDB(id);
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

        public string getBaseURI()
        {
            return "http://" + ipAddress + "/api/v2.0.0/";
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

            public void saveStatusToDB(int robotID)
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
                    query = "INSERT INTO error (CODE, DESCRIPTION, MODULE) VALUES ('";
                    query += errors. + "', '" + position.y + "', '" + position.orientation + "');";

                    Globals.issueInsertQuery(query);

                    position_id = Globals.getIDQuery("position");
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


                query = "INSERT INTO robot_status (ROBOT_ID, MODE_ID, STATE_ID, UPTIME, BATTERY_TIME_REMAINING, BATTERY_PERCENTAGE, DISTANCE_TO_NEXT_TARGET, MOVED, FOOTPRINT, joystick_low_speed_mode_enabled,"; // ALLOWED_METHODS,
                query += "joystick_web_session_id, map_id, mission_queue_id, mission_queue_url, mission_text, mode_key_state, mode_text, safety_system_muted, serial_number, session_id, "; //robot_model, robot_name, state_text, 
                query += "unloaded_map_changes, POSITION_ID, ERROR_ID, USER_PROMPT_ID, VELOCITY_ID) ";

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
                query += Globals.addToDB(footprint);
                query += Globals.addToDB(joystick_low_speed_mode_enabled);
                query += Globals.addToDB(joystick_web_session_id);
                query += Globals.addToDB(map_id);
                query += Globals.addToDB(mission_queue_id);
                query += Globals.addToDB(mission_queue_url);
                query += Globals.addToDB(MySqlHelper.EscapeString(mission_text));
                query += Globals.addToDB(mode_key_state);
                query += Globals.addToDB(mode_text);
                //query += Globals.addToDB(robot_model);
                //query += Globals.addToDB(robot_name);
                query += Globals.addToDB(safety_system_muted);
                query += Globals.addToDB(serial_number);
                query += Globals.addToDB(session_id);
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
    }
 
}
