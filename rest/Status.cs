﻿using System;
using System.Data;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using static Globals;

namespace Mirage.rest
{
    public class Status : IRest
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

        /// <summary>
        /// 
        /// </summary>
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

 /*                cmd.CommandText = "CALL store_status(@robotID, @pos_x, @pox_y, @pos_orientation, @velocity_lin, @velocity_ang, "
                                 + "@erroc_code, @error_desc, @error_module, @map_guid, @modeID, @stateID, @uptime, @batteryTimeRemaining, "
                                 + "@batteryPercentage, @distanceToTarget, @moved, @joystickLowSpeedMode, @joystickWebSessionID, @mission_queue_id, "
                                 + "@mission_text, @mode_text, @safetySystemMuted, @unloaded_map_changes, @user_prompt_id);";

                 cmd.Parameters.Add("@robotID", MySqlDbType.UInt64).Value = robotID;
                 cmd.Parameters.Add("@pos_x", MySqlDbType.Float).Value = position.x;
                 cmd.Parameters.Add("@pox_y", MySqlDbType.Float).Value = position.y;
                 cmd.Parameters.Add("@pos_orientation", MySqlDbType.Float).Value = position.orientation;
                 cmd.Parameters.Add("@velocity_lin", MySqlDbType.Float).Value = velocity.linear;
                 cmd.Parameters.Add("@velocity_ang", MySqlDbType.Float).Value = velocity.angular;

                 if(errors.Count > 0)
                 { 
                     cmd.Parameters.Add("@erroc_code", MySqlDbType.UInt64).Value = errors[0].code;
                     cmd.Parameters.Add("@error_desc", MySqlDbType.VarChar).Value = errors[0].description;
                     cmd.Parameters.Add("@error_module", MySqlDbType.VarChar).Value = errors[0].module;
                 }
                 else
                 {
                     cmd.Parameters.Add("@erroc_code", MySqlDbType.UInt64).Value = DBNull.Value;
                     cmd.Parameters.Add("@error_desc", MySqlDbType.VarChar).Value = DBNull.Value;
                     cmd.Parameters.Add("@error_module", MySqlDbType.VarChar).Value = DBNull.Value;
                 }

                 cmd.Parameters.Add("@map_guid", MySqlDbType.VarChar).Value = map_id;
                 cmd.Parameters.Add("@modeID", MySqlDbType.UInt64).Value = mode_id;
                 cmd.Parameters.Add("@stateID", MySqlDbType.UInt64).Value = state_id;
                 cmd.Parameters.Add("@uptime", MySqlDbType.UInt64).Value = uptime;
                 cmd.Parameters.Add("@batteryTimeRemaining", MySqlDbType.UInt64).Value = battery_time_remaining;
                 cmd.Parameters.Add("@batteryPercentage", MySqlDbType.Float).Value = battery_percentage;
                 cmd.Parameters.Add("@distanceToTarget", MySqlDbType.Float).Value = distance_to_next_target;
                 cmd.Parameters.Add("@moved", MySqlDbType.Float).Value = moved;
                 cmd.Parameters.Add("@joystickLowSpeedMode", MySqlDbType.Int32).Value = convertToInt(joystick_low_speed_mode_enabled);
                 cmd.Parameters.Add("@joystickWebSessionID", MySqlDbType.VarChar).Value = joystick_web_session_id;
                 cmd.Parameters.Add("@mission_queue_id", MySqlDbType.VarChar).Value = mission_queue_id;
                 cmd.Parameters.Add("@mission_text", MySqlDbType.VarChar).Value = mission_text;
                 cmd.Parameters.Add("@mode_text", MySqlDbType.VarChar).Value = mode_text;
                 cmd.Parameters.Add("@safetySystemMuted", MySqlDbType.Int32).Value = convertToInt(safety_system_muted);
                 cmd.Parameters.Add("@unloaded_map_changes", MySqlDbType.Int32).Value = convertToInt(unloaded_map_changes);
                 cmd.Parameters.Add("@user_prompt_id", MySqlDbType.UInt64).Value = user_prompt;*/

                //.Parameters.AddWithValue(

                issueQuery(cmd);
            }
            catch (Exception exception)
            {
                cmd.Dispose();
                Console.WriteLine(exception);
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
        /// Old way of saving status data.
        /// </summary>
        /// <param name="robotID"></param>
        /// <param name="m"></param>
        public void saveToDB(int robotID, List<Map> m)
        {
            Console.WriteLine("Saving To DB");

            MySqlCommand cmd = new MySqlCommand();

            try
            {
                //cmd.Connection = connection;
                cmd.CommandText = "CALL store_status(@robotID, @pos_x, @pox_y, @pos_orientation, @velocity_lin, @velocity_ang, "
                                + "@erroc_code, @error_desc, @error_module, @map_guid, @modeID, @stateID, @uptime, @batteryTimeRemaining, "
                                + "@batteryPercentage, @distanceToTarget, @moved, @joystickLowSpeedMode, @joystickWebSessionID, @mission_queue_id, "
                                + "@mission_text, @mode_text, @safetySystemMuted, @unloaded_map_changes, @user_prompt_id);";

                cmd.Parameters.Add("@robotID", MySqlDbType.Int32).Value = robotID;

                cmd.Parameters.Add("@pos_x", MySqlDbType.Float).Value = position.x;
                cmd.Parameters.Add("@pox_y", MySqlDbType.Float).Value = position.y;
                cmd.Parameters.Add("@pos_orientation", MySqlDbType.Float).Value = position.orientation;

                cmd.Parameters.Add("@velocity_lin", MySqlDbType.Float).Value = velocity.linear;
                cmd.Parameters.Add("@velocity_ang", MySqlDbType.Float).Value = velocity.angular;

                cmd.Parameters.Add("@erroc_code", MySqlDbType.Int32).Value = errors[0].code;
                cmd.Parameters.Add("@error_desc", MySqlDbType.VarChar).Value = errors[0].description;
                cmd.Parameters.Add("@error_module", MySqlDbType.VarChar).Value = errors[0].module;

                cmd.Parameters.Add("@map_guid", MySqlDbType.VarChar).Value = map_id;

                cmd.Parameters.Add("@modeID", MySqlDbType.Int32).Value = mode_id;
                cmd.Parameters.Add("@stateID", MySqlDbType.Int32).Value = state_id;
                cmd.Parameters.Add("@uptime", MySqlDbType.Int32).Value = uptime;
                cmd.Parameters.Add("@batteryTimeRemaining", MySqlDbType.Int32).Value = battery_time_remaining;
                cmd.Parameters.Add("@batteryPercentage", MySqlDbType.Float).Value = battery_percentage;
                cmd.Parameters.Add("@distanceToTarget", MySqlDbType.Float).Value = distance_to_next_target;
                cmd.Parameters.Add("@moved", MySqlDbType.Float).Value = moved;
                cmd.Parameters.Add("@joystickLowSpeedMode", MySqlDbType.Bit).Value = joystick_low_speed_mode_enabled;
                cmd.Parameters.Add("@joystickWebSessionID", MySqlDbType.VarChar).Value = joystick_web_session_id;
                cmd.Parameters.Add("@mission_queue_id", MySqlDbType.VarChar).Value = mission_queue_id;
                cmd.Parameters.Add("@mission_text", MySqlDbType.VarChar).Value = mission_text;
                cmd.Parameters.Add("@mode_text", MySqlDbType.VarChar).Value = mode_text;
                cmd.Parameters.Add("@safetySystemMuted", MySqlDbType.Bit).Value = safety_system_muted;
                cmd.Parameters.Add("@unloaded_map_changes", MySqlDbType.Bit).Value = unloaded_map_changes;
                cmd.Parameters.Add("@user_prompt_id", MySqlDbType.Int32).Value = user_prompt;

                //Console.WriteLine("To String:");
                //Console.WriteLine(cmd.ToString());

                logger(typeof(Status), DebugLevel.DEBUG, cmd.ToString());

                issueQuery(cmd);
            }
            catch (Exception exception)
            {
                logger(typeof(Status), DebugLevel.DEBUG, "Exception: ", exception);
                cmd.Dispose();

            }
            /*            int? position_id = null,
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
            */
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
            /*
                        query = "INSERT INTO robot_status (ROBOT_ID, MODE_ID, STATE_ID, UPTIME, BATTERY_TIME_REMAINING, BATTERY_PERCENTAGE, DISTANCE_TO_NEXT_TARGET, MOVED, joystick_low_speed_mode_enabled,"; // FOOTPRINT, ALLOWED_METHODS,
                        query += "joystick_web_session_id, map_id, mission_queue_id, mission_text, mode_text, safety_system_muted, "; //mission_queue_url, robot_model, robot_name, state_text, mode_key_state, serial_number, session_id,
                        query += "unloaded_map_changes, POSITION_ID, ERROR_CODE, USER_PROMPT_ID, VELOCITY_ID) ";
                        //--INSERT INTO robot_status (ROBOT_ID, MODE_ID, STATE_ID, UPTIME, BATTERY_TIME_REMAINING, BATTERY_PERCENTAGE, DISTANCE_TO_NEXT_TARGET, MOVED, joystick_low_speed_mode_enabled,joystick_web_session_id, map_id, mission_queue_id, mission_text, mode_text, safety_system_muted, unloaded_map_changes, POSITION_ID, ERROR_CODE, USER_PROMPT_ID, VELOCITY_ID)

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

                        try
                        {
                            for (int i = 0; i < m.Count; i++)
                            {
                                if (m[i].Guid == map_id)
                                    map_id = m[i].Map_id.ToString();
                            }
                        }
                        catch
                        {
                            map_id = "0";
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

                        Globals.issueInsertQuery(query);*/
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
