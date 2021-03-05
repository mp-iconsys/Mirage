using System;
using System.Data;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.rest
{
    public class Register : IRest
    {
        public int id { get; set; }
        public string label { get; set; }
        public string url { get; set; }
        public float value { get; set; }

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Register);

        /// <summary>
        /// 
        /// </summary>
        public void print()
        {
            logger(AREA, INFO, "");
            logger(AREA, INFO, "ID: " + id);
            logger(AREA, INFO, "Label: " + label);
            logger(AREA, INFO, "Url: " + url);
            logger(AREA, INFO, "Value: " + value);
            logger(AREA, INFO, "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveToMemory(HttpResponseMessage response)
        {
            Register temp = JsonConvert.DeserializeObject<Register>(response.Content.ReadAsStringAsync().Result);

            id = temp.id;
            label = temp.label;
            url = temp.url;
            value = temp.value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="robotID"></param>
        public void saveToDB(int robotID)
        {
            // TODO: This takes forever - make it better
            MySqlCommand cmd = new MySqlCommand("store_register");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("REGISTER_ID", id));
                cmd.Parameters.Add(new MySqlParameter("VALUE", value));

                issueQuery(cmd);
            }
            catch (Exception exception)
            {
                cmd.Dispose();
                logger(AREA, ERROR, "MySQL Quert Error: ", exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string getURL()
        {
            return url + id;
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
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest()
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
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage putRequest()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("")
            };

            return request;
        }
    }
}
