using System;
using System.Data;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.rest
{
    public class Status : IRest
    {
        public int mode_id { get; set; }
        public int state_id { get; set; }
        public int robot_group_id { get; set; }
        public int uptime { get; set; }
        public int battery_time_remaining { get; set; } // In seconds
        public float battery_percentage { get; set; }
        public float distance_to_next_target { get; set; }
        public float moved { get; set; }
        //public string allowed_methods { get; set; }
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


        public List<string> allowed_methods { get; set; }
        public Position position { get; set; }
        public List<ErrorsItem> errors { get; set; } // Try without a list
        public User_prompt user_prompt { get; set; }
        public Velocity velocity { get; set; }

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Status);

        public Status() 
        {
            robot_group_id = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        public void print()
        {
            logger(AREA, INFO, "");
            logger(AREA, INFO, "mode_id: " + mode_id);
            logger(AREA, INFO, "state_id: " + state_id);
            logger(AREA, INFO, "uptime: " + uptime);
            logger(AREA, INFO, "battery_time_remaining: " + battery_time_remaining);
            logger(AREA, INFO, "battery_percentage: " + battery_percentage);
            logger(AREA, INFO, "distance_to_next_target: " + distance_to_next_target);
            logger(AREA, INFO, "allowed_methods: " + allowed_methods);
            logger(AREA, INFO, "footprint: " + footprint);
            logger(AREA, INFO, "joystick_low_speed_mode_enabled: " + joystick_low_speed_mode_enabled);
            logger(AREA, INFO, "joystick_web_session_id: " + joystick_web_session_id);
            logger(AREA, INFO, "moved: " + moved);
            logger(AREA, INFO, "velocity linear: " + velocity.linear);
            logger(AREA, INFO, "velocity angular: " + velocity.angular);
            logger(AREA, INFO, "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
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

        /// <summary>
        /// Saves position, velocity, errors (if they exist) along with the status information
        /// of the robot.
        /// </summary>
        /// <param name="robotID">Specifies robot id</param>
        public void saveToDB(int robotID)
        {
            MySqlCommand cmd = new MySqlCommand("store_status");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("POS_X", position.x));
                cmd.Parameters.Add(new MySqlParameter("POS_Y", position.y));
                cmd.Parameters.Add(new MySqlParameter("POS_ORIENTATION", position.orientation));
                cmd.Parameters.Add(new MySqlParameter("VELOCITY_LIN", velocity.linear));
                cmd.Parameters.Add(new MySqlParameter("VELOCITY_ANG", velocity.angular));

                if (errors.Count > 0)
                {
                    cmd.Parameters.Add(new MySqlParameter("ERROR_CODE", errors[0].code));
                    cmd.Parameters.Add(new MySqlParameter("ERROR_DESCRIPTION", errors[0].description));
                    cmd.Parameters.Add(new MySqlParameter("ERROR_MODULE", errors[0].module));
                }
                else
                {
                    cmd.Parameters.Add(new MySqlParameter("ERROR_CODE", DBNull.Value));
                    cmd.Parameters.Add(new MySqlParameter("ERROR_DESCRIPTION", DBNull.Value));
                    cmd.Parameters.Add(new MySqlParameter("ERROR_MODULE", DBNull.Value));
                }

                cmd.Parameters.Add(new MySqlParameter("MAP_GUID", map_id));
                cmd.Parameters.Add(new MySqlParameter("MODE_ID", mode_id));
                cmd.Parameters.Add(new MySqlParameter("STATE_ID", state_id));
                cmd.Parameters.Add(new MySqlParameter("UPTIME", uptime));
                cmd.Parameters.Add(new MySqlParameter("BATTER_TIME", battery_time_remaining));
                cmd.Parameters.Add(new MySqlParameter("BATTERY_PERCENTAGE", battery_percentage));
                cmd.Parameters.Add(new MySqlParameter("DISTANCE_TO_TARGET", distance_to_next_target));
                cmd.Parameters.Add(new MySqlParameter("MOVED", moved));
                cmd.Parameters.Add(new MySqlParameter("JOYSTICK_LOW_SPEED_MODE", convertToInt(joystick_low_speed_mode_enabled)));
                cmd.Parameters.Add(new MySqlParameter("JOYSTICK_WEB_SESSION", joystick_web_session_id));
                cmd.Parameters.Add(new MySqlParameter("MISSION_QUEUE_ID", mission_queue_id));
                cmd.Parameters.Add(new MySqlParameter("MISSION_TEXT", mission_text));
                cmd.Parameters.Add(new MySqlParameter("MODE_TEXT", mode_text));
                cmd.Parameters.Add(new MySqlParameter("SAFETY_SYSTEM_MUTED", convertToInt(safety_system_muted)));
                cmd.Parameters.Add(new MySqlParameter("UNLOADED_MAP_CHANGES", convertToInt(unloaded_map_changes)));
                cmd.Parameters.Add(new MySqlParameter("USER_PROMPT_ID", user_prompt));

                issueQuery(cmd);
            }
            catch (Exception exception)
            {
                cmd.Dispose();
                logger(AREA, ERROR, "MySQL Quert Error: ", exception);
            }
        }

        /// <summary>
        /// Helper function for converting a boolean string (false or true) into an integer
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        private int convertToInt(string a)
        {
            if (a == "true") { return 1; }
            else if (a == "false") { return 0; }
            else { return 2; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="robotID"></param>
        public void saveAll(HttpResponseMessage response, int robotID)
        {
            saveToMemory(response);
            saveToDB(robotID);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Status does not have a delete option for Mirs
        /// </summary>
        /// <returns></returns> 
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Nested class, used to contain position data.
        /// </summary>
        public class Position
        {
            public double x { get; set; }
            public double y { get; set; }
            public double orientation { get; set; }
        }

        /// <summary>
        /// Nested class, used to contain errors.
        /// </summary>
        public class ErrorsItem
        {
            public int code { get; set; }
            public string description { get; set; }
            public string module { get; set; }
        }

        /// <summary>
        /// Nested class, used to contain velocity data.
        /// </summary>
        public class Velocity
        {
            public double linear { get; set; }
            public double angular { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class User_prompt
        {
            public string guid { get; set; }
            public string question { get; set; }
            public List<string> options { get; set; }
            public int timeout { get; set; }
            public string user_group { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
        public class Cart
        {
            public int id { get; set; }
            public int length { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int offset_locked_wheels { get; set; }
        }

        /// <summary>
        /// 
        /// </summary>
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
