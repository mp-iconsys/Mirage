using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Net.Http;
using System.Text;
using static Globals;

namespace Mirage.rest
{
    public class SoftwareLog : IRest
    {
        public string Action { get; set; }
        public string End_time { get; set; }
        public string From { get; set; }
        public string Guid { get; set; }
        public string Start_time { get; set; }
        public string State { get; set; }
        public string To { get; set; }
        public string Url { get; set; }

        /// <summary>
        /// 
        /// </summary>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="robotID"></param>
        public void saveToDB(int robotID)
        {
            MySqlCommand cmd = new MySqlCommand("store_maps");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("FROM", From));
                cmd.Parameters.Add(new MySqlParameter("TO", To));
                cmd.Parameters.Add(new MySqlParameter("ACTION", Action));
                cmd.Parameters.Add(new MySqlParameter("STATE", State));
                cmd.Parameters.Add(new MySqlParameter("START_TIME", Start_time));
                cmd.Parameters.Add(new MySqlParameter("END_TIME", End_time));
                cmd.Parameters.Add(new MySqlParameter("URL", Url));
                cmd.Parameters.Add(new MySqlParameter("GUID", Guid));

                issueQuery(cmd);
            }
            catch (Exception exception)
            {
                cmd.Dispose();
                Console.WriteLine(exception);
            }
/*
            string query = "REPLACE INTO software_logs (`ROBOT_ID`, `FROM`, `TO`, `ACTION`, `STATE`, `START_TIME`, `END_TIME`, `URL`, `GUID`) VALUES ";

            query += "('" + robotID + "', '" + From + "', '" + To + "', '" + Action + "', '" + State + "', '" + Start_time
                      + "', '" + End_time + "', '" + Url + "', '" + Guid + "');";

            Globals.issueInsertQuery(query);*/
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
            string payload = "stuff";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri("status/")
            };

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
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
