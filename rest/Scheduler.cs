using System;
using System.Data;
using System.Text;
using System.Net.Http;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;
using Newtonsoft.Json;

namespace Mirage.rest
{
    public class Scheduler : IRest
    {
        public string url { get; set; }
        public string state { get; set; }
        public int state_id { get; set; }
        public string mission { get; set; }
        public string mission_id { get; set; }
        public int mission_number { get; set; }
        public string description { get; set; }
        public int id { get; set; }
        public int robot_id { get; set; }

        public bool working_response;

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Scheduler);

        public Scheduler() 
        {
            id = 0;
            state_id = 0;
        }

        /// <summary>
        /// Prints map data from memory to the log file and console.
        /// </summary>
        public void print()
        {
            logger(AREA, INFO, "");
            logger(AREA, INFO, "==== Printing Scheduler Data ====");
            logger(AREA, INFO, "URL: " + url);
            logger(AREA, INFO, "ID: " + id);
            logger(AREA, INFO, "State: " + state);
            logger(AREA, INFO, "robot_id: " + robot_id);
            logger(AREA, INFO, "mission: " + mission);
            logger(AREA, INFO, "mission_id: " + mission_id);
            logger(AREA, INFO, "description: " + description);
            logger(AREA, INFO, "==== Finished Scheduler Print ====");
            logger(AREA, INFO, "");
        }

        public void saveToMemory(HttpResponseMessage response) 
        {
            Scheduler temp = JsonConvert.DeserializeObject<Scheduler>(response.Content.ReadAsStringAsync().Result);

            state = temp.state;
            mission = temp.mission;
            mission_id = temp.mission_id;
            description = temp.description;
            id = temp.id;
            robot_id = temp.robot_id;
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
                RequestUri = new Uri("fire_alarms")
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
                RequestUri = new Uri("fire_alarms")
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
            string payload = "";
            string url = "http://192.168.1.195/api/v2.0.0/robots/";
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
            string payload ="";
            string url = "http://127.0.0.1:15151/api/v2.0.0/robots/" + robot;
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
