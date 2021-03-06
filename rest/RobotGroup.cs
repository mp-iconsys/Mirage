﻿using System;
using System.Data;
using System.Text;
using System.Net.Http;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;
using Newtonsoft.Json;

namespace Mirage.rest
{
    public class RobotGroup : IRest
    {
        public bool active { get; set; }
        public string description { get; set; } // Min length 3, Max length 255
        public int robot_group_id { get; set; }
        public string status { get; set; }
        public string fleet_state_text { get; set; }
        public string ip { get; set; }
        public int fleet_state { get; set; }
        public string created_by { get; set; }
        public string serial_number { get; set; }
        public string created_by_id { get; set; }
        public int id { get; set; }
        public string robot_model { get; set; }
        public int robot_group_mapping { get; set; }

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(RobotGroup);

        public RobotGroup() { }

        /// <summary>
        /// Prints map data from memory to the log file and console.
        /// </summary>
        public void print()
        {
            logger(AREA, INFO, "");
            logger(AREA, INFO, "==== Printing RobotGroup Data ====");
            logger(AREA, INFO, "Created_by: " + created_by);
            logger(AREA, INFO, "Created_by_id: " + created_by_id);
            logger(AREA, INFO, "serial_number: " + serial_number);
            logger(AREA, INFO, "id: " + id);
            logger(AREA, INFO, "robot_model: " + robot_model);
            logger(AREA, INFO, "fleet_state: " + fleet_state);
            logger(AREA, INFO, "ip: " + ip);
            logger(AREA, INFO, "fleet_state_text: " + fleet_state_text);
            logger(AREA, INFO, "status: " + status);
            logger(AREA, INFO, "robot_group_id: " + robot_group_id);
            logger(AREA, INFO, "description: " + description);
            logger(AREA, INFO, "active: " + active);
            logger(AREA, INFO, "==== Finished RobotGroup Print ====");
            logger(AREA, INFO, "");
        }

        public void saveToMemory(HttpResponseMessage response) 
        {
            RobotGroup temp = JsonConvert.DeserializeObject<RobotGroup>(response.Content.ReadAsStringAsync().Result);

            id = temp.id;
            robot_group_id = temp.robot_group_id;
        }

        /// <summary>
        /// Saves RobotGroup data to the database.
        /// </summary>
        /// <param name="robotID">ID of the polled robot</param>
        public void saveToDB(int robotID)
        {
            /*
            MySqlCommand cmd = new MySqlCommand("store_maps");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new MySqlParameter("MAP_ID", Map_id));
                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("NAME", Name));
                cmd.Parameters.Add(new MySqlParameter("GUID", Guid));
                cmd.Parameters.Add(new MySqlParameter("CREATED_BY_NAME", Created_by_name));
                cmd.Parameters.Add(new MySqlParameter("CREATED_BY_ID", Created_by_id));
                cmd.Parameters.Add(new MySqlParameter("MAP", map));
                cmd.Parameters.Add(new MySqlParameter("METADATA", Metadata));
                cmd.Parameters.Add(new MySqlParameter("ONE_WAY_MAP", One_way_map));
                cmd.Parameters.Add(new MySqlParameter("ORIGIN_THETA", Origin_theta));
                cmd.Parameters.Add(new MySqlParameter("ORIGIN_X", Origin_x));
                cmd.Parameters.Add(new MySqlParameter("ORIGIN_Y", Origin_y));
                cmd.Parameters.Add(new MySqlParameter("PATH_GUIDES", Path_guides));
                cmd.Parameters.Add(new MySqlParameter("PATHS", Paths));
                cmd.Parameters.Add(new MySqlParameter("POSITIONS", Positions));
                cmd.Parameters.Add(new MySqlParameter("RESOLUTION", Resolution));

                issueQuery(cmd);
            }
            catch (Exception exception)
            {
                cmd.Dispose();
                logger(AREA, ERROR, "MySQL Quert Error: ", exception);
            }
            */
        }

        public void saveAll(HttpResponseMessage response, int id)
        {

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
                RequestUri = new Uri("robot_groups")
            };

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("robot_groups")
            };

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest(string name, string description ,string allow_all, string created_by)
        {
            string payload;
            payload = "{\"name\": \"" + name + "\", ";
            payload += "\"description\": \"" + description + "\", ";
            payload += "\"allow_all_mission_groups:\": " + allow_all + "}";
            //payload += "\"created_by_id\": \"" + created_by + "\"}";

            Console.WriteLine(payload);

            string url = "http://" + fleetManagerIP + "/api/v2.0.0/robot_groups/";
            Uri uri = new Uri(url);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = uri
            };

            return request;
        }

        /// <summary>
        /// Looks like:
        /// {
        ///     "alarm_on": true, (can be true or false)
        ///     "note": "string",
        ///     "trigger_time": "2021-01-08T10:25:46.578Z"
        /// }
        /// </summary>
        /// <param name="id">ID of the fire alarm that will be triggered</param>
        /// <returns>An HttpRequestMessage that will put new data</returns>
        public HttpRequestMessage putRequest()
        {
            string payload;
            payload = "{\"alarm_on\": " + description+ ", ";
            payload += "\"active\": "   + active.ToString().ToLowerInvariant() + ", ";
            payload += "\"robot_group_id\": " + robot_group_id + "}";


            string  url = "http://"+fleetManagerIP+"/api/v2.0.0/robots/";
                    url += 1;

            Uri uri = new Uri(url);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = uri
            };

            return request;
        }

        /// <summary>
        /// Assigns a robot (given by the robot parameter) to a particulart group (given by robot_group)
        /// </summary>
        /// <param name="robot">Robot ID in the fleet</param>
        /// <param name="robot_group_id">Robot Group ID in the fleet</param>
        /// <returns>An HttpRequestMessage that will put new data</returns>
        public HttpRequestMessage putRequest(int robot, int robot_group_id)
        {
            string payload;
            payload = "{\"robot_group_id\": " + robot_group_id + "}";

            //payload = "{\"alarm_on\": " + description + ", ";
            //payload += "\"active\": " + active.ToString().ToLowerInvariant() + ", ";
            //payload += "\"robot_group_id\": " + robot_group_id + "}";

            string url = "http://"+fleetManagerIP+"/api/v2.0.0/robots/" + robot;
            Uri uri = new Uri(url);


            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = uri
            };

            //logger(AREA, INFO, request.ToString());
            //logger(AREA, INFO, request.Content.ReadAsStringAsync().Result);

            return request;
        }

    }
}
