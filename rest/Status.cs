using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Mirage.rest
{
    class Status : IRest
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

        public void print()
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

        public void saveToMemory(HttpResponseMessage response) 
        { 
            Status temp = JsonConvert.DeserializeObject<Status>(response.Content.ReadAsStringAsync().Result);
            
            mode_id = temp.mode_id;
            state_id = temp.state_id;
            uptime = temp.uptime;
            battery_time_remaining = temp.battery_time_remaining; // In seconds
            battery_percentage = temp.battery_percentage;
            distance_to_next_target = temp.distance_to_next_target;
            moved = temp.moved;
            allowed_methods = temp.allowed_methods;
            footprint = temp.footprint;
            joystick_low_speed_mode_enabled = temp.joystick_low_speed_mode_enabled;
            joystick_web_session_id = temp.joystick_web_session_id;
            map_id = temp.map_id;
            mission_queue_id = temp.mission_queue_id;
            mission_queue_url = temp.mission_queue_url;
            mission_text = temp.mission_text;
            mode_key_state = temp.mode_key_state;
            mode_text = temp.mode_text;
            robot_model = temp.robot_model;
            robot_name = temp.robot_name;
            safety_system_muted = temp.safety_system_muted;
            serial_number = temp.serial_number;
            session_id = temp.session_id;
            state_text = temp.state_text;
            unloaded_map_changes = temp.unloaded_map_changes;

            position = temp.position;
            errors = temp.errors; // Try without a list
            user_prompt = temp.user_prompt;
            velocity = temp.velocity;
    }

        public void saveToDB() 
        { 
            // Use a transaction here
        }

        public void saveToDB(int robotID)
        {
            // Use a transaction here
        }

        public void saveToDB(int robotID, List<Map> m)
        {
            int? position_id = null,
                    error_id = null,
                    user_promt_id = null,
                    velocity_id = null;
            string query;

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
                if (errors.Count == 0 || !errors.Any())
                {
                    if (Globals.debugLevel > 1)
                        Console.WriteLine("==== Robot " + robotID + " Has No Errors ====");
                }
                else
                {
                    query = "REPLACE INTO error (CODE, DESCRIPTION, MODULE) VALUES ";

                    int i;

                    // Assume we only have one error here - no need to faff around
                    for (i = 0; i < (errors.Count - 1); i++)
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

            for (int i = 0; i < m.Count; i++)
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
            catch (System.ArgumentNullException e)
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

        public void saveAll(HttpResponseMessage response, int robotID)
        {
            saveToMemory(response);
            saveToDB(robotID);
        }

        // Status does not have a delete option for Mirs
        public HttpRequestMessage deleteRequest()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("")
            };

            return request;
        }

        // Status does not have a delete option for Mirs
        public HttpRequestMessage postRequest()
        {
            string payload = "stuff";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri("status/")
            };

            return request;
        }

        public HttpRequestMessage putRequest()
        {
            string payload = "stuff";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = new Uri("mission_scheduler")
            };

            return request;
        }

        //  Nested Classes, to contain data in a nicer way
        // 
        // 
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
