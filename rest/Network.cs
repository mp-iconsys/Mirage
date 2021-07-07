using System;
using System.Data;
using System.Text;
using System.Net.Http;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;
using Newtonsoft.Json
    ;
using System.Collections.Generic;

namespace Mirage.rest
{
    public class Network : IRest
    {
        /// <summary>
        /// 
        /// </summary>
        public int strength { get; set; }
        public List<string> allowed_methods { get; set; }
        public string ssid { get; set; }
        public string url { get; set; }
        public string frequency { get; set; }
        public string connected { get; set; }
        public string device { get; set; }
        public string security { get; set; }
        public string guid { get; set; }
        public int channel { get; set; }

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(RobotGroup);

        public Network() 
        {
            strength = 0;
            ssid = "";
            url = "";
            frequency = "";
            connected = "";
            device = "";
            security = "";
            guid = "";
            channel = 0;
            allowed_methods = new List<string>();
        }

        /// <summary>
        /// Prints map data from memory to the log file and console.
        /// </summary>
        public void print()
        {
            logger(AREA, INFO, "");
            logger(AREA, INFO, "==== Printing WiFi Network Data ====");
            logger(AREA, INFO, "Strength: " + strength);
            logger(AREA, INFO, "SSID: " + ssid);
            logger(AREA, INFO, "URL: " + url);
            logger(AREA, INFO, "frequency " + frequency);
            logger(AREA, INFO, "connected: " + connected);
            logger(AREA, INFO, "device: " + device);
            logger(AREA, INFO, "security: " + security);
            logger(AREA, INFO, "guid: " + guid);
            logger(AREA, INFO, "channel: " + channel);
            logger(AREA, INFO, "==== Finished WiFi Network Print ====");
            logger(AREA, INFO, "");
        }

        public void saveToMemory(HttpResponseMessage response)
        {
            Network temp = JsonConvert.DeserializeObject<Network>(response.Content.ReadAsStringAsync().Result);

            strength = temp.strength;
            allowed_methods = temp.allowed_methods;
            ssid = temp.ssid;
            url = temp.url;
            frequency = temp.frequency;
            connected = temp.connected;
            device = temp.device;
            security = temp.security;
            guid = temp.guid;
            channel = temp.channel;
        }

        public void saveStrengthAndChannel(HttpResponseMessage response)
        {
            Network temp = JsonConvert.DeserializeObject<Network>(response.Content.ReadAsStringAsync().Result);
            strength = temp.strength;
            channel = temp.channel;
        }

        /// <summary>
        /// Saves RobotGroup data to the database.
        /// </summary>
        /// <param name="robotID">ID of the polled robot</param>
        public void saveToDB(int robotID)
        {
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
            logger(AREA, INFO, "==== WiFi Network Delete Request Is Empty ====");

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("")
            };

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest()
        {
            logger(AREA, INFO, "==== WiFi Network Post Request Is Empty ====");

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri("")
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
            logger(AREA, INFO, "==== WiFi Network PUT Request Is Empty ====");

            string payload = "";
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
            logger(AREA, INFO, "==== WiFi Network PUT Request Is Empty ====");

            string payload = "";
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
    }
}
