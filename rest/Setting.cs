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
    public class Setting : IRest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Parent_name { get; set; }
        public string Url { get; set; }
        public string Value { get; set; }
        public string Default { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void print()
        {
            Console.WriteLine("ID: " + Id);
            Console.WriteLine("Name: " + Name);
            Console.WriteLine("Parent_name: " + Parent_name);
            Console.WriteLine("Url: " + Url);
            Console.WriteLine("Value: " + Value);
            Console.WriteLine("Default: " + Default);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveToMemory(HttpResponseMessage response)
        {
            Setting temp = JsonConvert.DeserializeObject<Setting>(response.Content.ReadAsStringAsync().Result);

            Id = temp.Id;
            Name = temp.Name;
            Parent_name = temp.Parent_name;
            Url = temp.Url;
            Value = temp.Value;
            Default = temp.Default;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="robotID"></param>
        public void saveToDB(int robotID)
        {
            MySqlCommand cmd = new MySqlCommand("store_settings");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new MySqlParameter("SETTING_ID", Id));
                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("NAME", Name));
                cmd.Parameters.Add(new MySqlParameter("PARENT_NAME", Parent_name));
                cmd.Parameters.Add(new MySqlParameter("URL", Url));
                cmd.Parameters.Add(new MySqlParameter("VALUE", Value));
                cmd.Parameters.Add(new MySqlParameter("DEFAULT_VALUE", Default));

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
