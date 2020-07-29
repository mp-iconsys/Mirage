using System;
using System.Data;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using static Globals;

namespace Mirage.rest
{
    public class Register : IRest
    {
        public int id { get; set; }
        public string label { get; set; }
        public string url { get; set; }
        public float value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void print()
        {
            Console.WriteLine("ID: " + id);
            Console.WriteLine("Label: " + label);
            Console.WriteLine("url: " + url);
            Console.WriteLine("value: " + value);
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
                Console.WriteLine(exception);
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
