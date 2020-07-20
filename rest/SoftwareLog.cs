using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;

namespace Mirage.rest
{
    class SoftwareLog : IRest
    {
        private string Action { get; set; }
        private string End_time { get; set; }
        private string From { get; set; }
        private string Guid { get; set; }
        private string Start_time { get; set; }
        private string State { get; set; }
        private string To { get; set; }
        private string Url { get; set; }

        public void print()
        {
            Console.WriteLine("Action: " + Action);
            Console.WriteLine("End_time: " + End_time);
            Console.WriteLine("From: " + From);
            Console.WriteLine("Guid: " + Guid);
            Console.WriteLine("Start_time: " + Start_time);
            Console.WriteLine("State: " + State);
            Console.WriteLine("To: " + To);
            Console.WriteLine("Url: " + Url);
        }

        public void saveToMemory(HttpResponseMessage response)
        {
            SoftwareLog temp = JsonConvert.DeserializeObject<SoftwareLog>(response.Content.ReadAsStringAsync().Result);

            Action = temp.Action;
            End_time = temp.End_time;
            From = temp.From;
            Guid = temp.Guid;
            Start_time = temp.Start_time;
            State = temp.State;
            To = temp.To;
            Url = temp.Url;
        }

        public void saveToDB(int robotID)
        {
            string query = "REPLACE INTO software_logs (`ROBOT_ID`, `FROM`, `TO`, `ACTION`, `STATE`, `START_TIME`, `END_TIME`, `URL`, `GUID`) VALUES ";

            query += "('" + robotID + "', '" + From + "', '" + To + "', '" + Action + "', '" + State + "', '" + Start_time
                      + "', '" + End_time + "', '" + Url + "', '" + Guid + "');";

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
            string payload = "stuff";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri("status/")
            };

            return request;
        }

        public HttpRequestMessage putRequest()
        {
            string payload = "stuff";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = new Uri("mission_scheduler")
            };

            return request;
        }

    }
}
