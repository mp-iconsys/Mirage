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
    public class ChargingGroup : IRest
    {
        public string url { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        private string url_base = "http://" + fleetManagerIP + "/api/v2.0.0/charging_groups/";

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(ChargingGroup);

        public ChargingGroup()
        {
            url = "";
            id = 0;
            name = "";
        }

        public ChargingGroup(int id, string name, string url)
        {
            this.url = url;
            this.id = id;
            this.name = name;
        }

        /// <summary>
        /// Prints the charging group info
        /// </summary>
        public void print()
        {
            logger(AREA, DEBUG, "");
            logger(AREA, DEBUG, "Charging Group : " + id + " Called: " + name);
            logger(AREA, DEBUG, "URL: " + url);
            logger(AREA, DEBUG, "");
        }

        /// <summary>
        /// Save the HTTP Response to internal memory
        /// </summary>
        /// <param name="response">HTTP Response</param>
        public void saveToMemory(HttpResponseMessage response)
        {
            ChargingGroup temp = JsonConvert.DeserializeObject<ChargingGroup>(response.Content.ReadAsStringAsync().Result);

            id = temp.id;
            name = temp.name;
            url = temp.url;
        }

        /// <summary>
        /// Saves ChargingGroup data to the database.
        /// </summary>
        /// <param name="robotID">ID of the polled robot</param>
        public void saveToDB(int robotID)
        {
        }

        /// <summary>
        /// Blank method to fit the interface
        /// </summary>
        /// <param name="response"></param>
        /// <param name="id"></param>
        public void saveAll(HttpResponseMessage response, int id)
        {

        }

        /// <summary>
        /// Empty Function to fit the interface - do not use
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage deleteRequest()
        {
            int fleetRobotID = 2;
            url_base = "http://" + fleetManagerIP + "/api/v2.0.0/charging_groups/";
            // Full URL is: /charging_groups/{group_id}/robots /{robot_id}
            string url_address = url_base + id + "/robots/" + fleetRobotID;

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(url_address)
            };

            return request;
        }

        /// <summary>
        /// Used to remove a robot from a charging group
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage deleteRequest(int fleetRobotID)
        {
            // Full URL is: /charging_groups/{group_id}/robots /{robot_id}
            string url_address = url_base + id + "/robots/" + fleetRobotID;

            logger(AREA, DEBUG, "The URL is: " + url_address);

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri(url_address)
            };

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest()
        {
            // Full URL is: /charging_groups/{group_id}/robots /{robot_id}
            string url_address = url_base + id + "/robots/";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("robot_groups")
            };

            return request;
        }

        /// <summary>
        /// Assigns a robot given by fleetRobotID to the charging group
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest(int fleetRobotID)
        {
            string payload;
            payload = "{\"robot_id\": " + fleetRobotID + ", ";
            payload += "\"group_id\": " + id + " }";

            logger(AREA, DEBUG, payload);

            url_base = "http://" + fleetManagerIP + "/api/v2.0.0/charging_groups/";
            string url = url_base + id + "/robots";
            Uri uri = new Uri(url);

            logger(AREA, DEBUG, payload);
            logger(AREA, DEBUG, url);

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
            string payload = "";
            //payload = "{\"alarm_on\": " + description + ", ";
            //payload += "\"active\": " + active.ToString().ToLowerInvariant() + ", ";
            //payload += "\"robot_group_id\": " + robot_group_id + "}";


            string url = "http://" + fleetManagerIP + "/api/v2.0.0/robots/";
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
            string payload = "";
           // payload = "{\"alarm_on\": " + description + ", ";
            //payload += "\"active\": " + active.ToString().ToLowerInvariant() + ", ";
            payload += "\"robot_group_id\": " + robot_group_id + "}";

            string url = "http://" + fleetManagerIP + "/api/v2.0.0/robots/" + robot;
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
