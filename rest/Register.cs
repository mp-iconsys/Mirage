using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Mirage.rest
{
    public class Register : IRest
    {
        public int id { get; set; }
        public string label { get; set; }
        public string url { get; set; }
        public float value { get; set; }

        public void print()
        {
            Console.WriteLine("ID: " + id);
            Console.WriteLine("Label: " + label);
            Console.WriteLine("url: " + url);
            Console.WriteLine("value: " + value);
        }

        public void saveToMemory(HttpResponseMessage response)
        {
            Register temp = JsonConvert.DeserializeObject<Register>(response.Content.ReadAsStringAsync().Result);

            id = temp.id;
            label = temp.label;
            url = temp.url;
            value = temp.value;
        }

        public void saveToDB(int robotID)
        {
            string query = "REPLACE INTO registers (`ROBOT_ID`, `REGISTER_ID`, `VALUE`) VALUES ";
                   query += "('" + robotID + "','" + id + "','" + value + "');";

            //logger(typeof(Setting), DEBUG, query);

            Globals.issueInsertQuery(query);
        }

        public string getURL()
        {
            return url + id;
        }

        public void saveAll(HttpResponseMessage response, int robotID)
        {
            saveToMemory(response);
            saveToDB(robotID);
        }

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
