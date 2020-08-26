using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Net.Http;
using System.Text;
using static Globals;
using static Globals.DebugLevel;

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

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(SoftwareLog);

        /// <summary>
        /// 
        /// </summary>
        public void print()
        {
            logger(AREA, INFO, "");
            logger(AREA, INFO, "Action: " + Action);
            logger(AREA, INFO, "End_time: " + End_time);
            logger(AREA, INFO, "From: " + From);
            logger(AREA, INFO, "Guid: " + Guid);
            logger(AREA, INFO, "Start_time: " + Start_time);
            logger(AREA, INFO, "State: " + State);
            logger(AREA, INFO, "To: " + To);
            logger(AREA, INFO, "Url: " + Url);
            logger(AREA, INFO, "");
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
            MySqlCommand cmd = new MySqlCommand("store_software_logs");

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
                logger(AREA, ERROR, "MySQL Quert Error: ", exception);
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
