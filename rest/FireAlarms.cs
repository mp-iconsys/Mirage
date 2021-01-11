using System;
using System.Data;
using System.Text;
using System.Net.Http;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.rest
{
    public class FireAlarms : IRest
    {
        public int id;
        public string note;
        public bool alarm_on;
        public string trigger_time;

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(FireAlarms);

        public FireAlarms() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="missionNumber"></param>
        public FireAlarms(int missionNumber)
        {
        }


        public void print()
        {

        }

        public void saveToMemory(HttpResponseMessage response)
        {

        }

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
            string payload;
            payload = "{\"alarm_on\": " + alarm_on.ToString().ToLowerInvariant() + ", ";
            payload += "\"note\": \"test\", ";
            payload += "\"trigger_time\": \"" + DateTime.Now.ToString("s") + "Z\"}";

            string uri = "fire_alarms/1";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = new Uri(uri)
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
        public HttpRequestMessage putRequest(bool alarm_on, int id)
        {
            string  payload;
                    payload  = "{\"alarm_on\": " + alarm_on.ToString().ToLowerInvariant() + ", ";
                    payload += "\"note\": \"test\", ";
                    payload += "\"trigger_time\": \"" + DateTime.Now.ToString("s") + "Z\"}";

            //string uri = "fire_alarms/" + id;
            //string ur = "http://192.168.1.195/api/v2.0.0/fire_alarms/1";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = new Uri("http://192.168.1.195/api/v2.0.0/fire_alarms/1")
            };

            logger(AREA, INFO, request.ToString());
            logger(AREA, INFO, request.Content.ReadAsStringAsync().Result);

            return request;
        }

    }
}
