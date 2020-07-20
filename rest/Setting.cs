using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace Mirage.rest
{
    class Setting : IRest
    {
        private int Id { get; set; }
        private string Name { get; set; }
        private string Parent_name { get; set; }
        private string Url { get; set; }
        private string Value { get; set; }
        private string Default { get; set; }

        public void print()
        {
            Console.WriteLine("ID: " + Id);
            Console.WriteLine("Name: " + Name);
            Console.WriteLine("Parent_name: " + Parent_name);
            Console.WriteLine("Url: " + Url);
            Console.WriteLine("Value: " + Value);
            Console.WriteLine("Default: " + Default);
        }

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

        public void saveToDB(int robotID)
        {
            string query = "REPLACE INTO settings (`SETTING_ID`, `ROBOT_ID`, `NAME`, `PARENT_NAME`, `URL`, `VALUE`, `DEFAULT_VALUE`) VALUES ";

            query += "('" + Id + "','" + robotID + "','" + Name + "','" + Parent_name + "','" + Url + "','" + Value + "','" + Default + "'),";

            Globals.issueInsertQuery(query);
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
