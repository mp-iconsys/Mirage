using System;
using System.Data;
using System.Text;
using System.Net.Http;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;
using Newtonsoft.Json;

namespace Mirage.rest
{
    public class Mission : IRest
    {
        public int missionNumber { get; set; }
        public string guid { get; set; }// Add mission number to that
        public string hidden { get; set; }
        public string group_id { get; set; }
        public string description { get; set; }
        public string created_by_id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string url_string { get; set; }
        public string uri_suffix { get; set; }
        public string missionNumberString { get; set; }

    //=========================================================|
    //  Used For Logging & Debugging                           |     
    //=========================================================|
    private static readonly Type AREA = typeof(Mission);

        public Mission() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="missionNumber"></param>
        public Mission(int missionNumber)
        {
            this.missionNumber = missionNumber;

            if (missionNumber < 10)
                this.missionNumberString = "0" + missionNumber;
            else
                this.missionNumberString = missionNumber.ToString();

            guid = guid + missionNumberString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="missionNumber"></param>
        /// <returns></returns>
        public string stringyfyMission(int missionNumber)
        {
            if (missionNumber < 10)
                return "0" + missionNumber;
            else
                return missionNumber.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public void print()
        {
            logger(AREA, DEBUG, "");
            logger(AREA, DEBUG, "Mission No: " + missionNumber);
            logger(AREA, DEBUG, "GUID: " + guid);
            logger(AREA, DEBUG, "Name: " + name);
            logger(AREA, DEBUG, "Url: " + url);
            logger(AREA, DEBUG, "URI Suffix: " + uri_suffix);
            logger(AREA, DEBUG, "missionNumberString: " + missionNumberString);
            logger(AREA, DEBUG, "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveToMemory(HttpResponseMessage response)
        {
            Mission temp = JsonConvert.DeserializeObject<Mission>(response.Content.ReadAsStringAsync().Result);

            guid = temp.guid;
            name = temp.name;
            url = temp.url;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="robotID"></param>
        public void saveToDB(int robotID, int missionNo)
        {
            missionNumber = missionNo;
            this.missionNumber = missionNo;

            MySqlCommand cmd = new MySqlCommand("store_missions");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new MySqlParameter("MISSION_ID", missionNumber));
                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("GUID", guid));
                cmd.Parameters.Add(new MySqlParameter("NAME", name));
                cmd.Parameters.Add(new MySqlParameter("URL", url));

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
        /// <param name="robotID"></param>
        public void saveToDB(int robotID)
        {
            getMissionNumber(guid);

            MySqlCommand cmd = new MySqlCommand("store_missions");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new MySqlParameter("MISSION_ID", missionNumber));
                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("GUID", guid));
                cmd.Parameters.Add(new MySqlParameter("NAME", name));
                cmd.Parameters.Add(new MySqlParameter("URL", url));

                issueQuery(cmd);
            }
            catch (Exception exception)
            {
                cmd.Dispose();
                logger(AREA, ERROR, "MySQL Quert Error: ", exception);
            }
        }

        /// <summary>
        /// Extracts the mission number as a string and integer from the GUID.
        /// </summary>
        /// <param name="guid"></param>
        private void getMissionNumber(string guid)
        {
            try
            { 
                missionNumberString = guid.Substring(19, 4);
                missionNumber = Int32.Parse(missionNumberString);
            }
            catch
            {

                missionNumberString = "0011";
                missionNumber = 11;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        /// <param name="robotID"></param>
        public void saveAll(HttpResponseMessage response, int robotID)
        {

        }

        /// <summary>
        /// Create mission
        /// </summary>
        /// <param name="missionNumber"></param>
        /// <returns></returns>
        public HttpRequestMessage createMission(int missionNumber)
        {
            string payload = "{\r\n  \"guid\": \"" + guid + stringyfyMission(missionNumber) + "\",\r\n  \"name\": \"Mission" + stringyfyMission(missionNumber) + "\",\r\n  \"description\": \"template_mission_" + stringyfyMission(missionNumber) + "\",\r\n  \"hidden\": false,\r\n  \"group_id\": \"mirconst-guid-0000-0011-missiongroup\",\r\n  \"session_id\": \"caa94ad9-65cc-11e9-abc1-94c691a7361d\",\r\n  \"created_by_id\": \"mirconst-guid-0000-0004-users0000000\"\r\n}";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri("mission_scheduler")
            };

            return request;
        }

        /// <summary>
        /// Clear schedule
        /// </summary>
        public HttpRequestMessage deleteRequest()
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Delete,
                RequestUri = new Uri("mission_scheduler")
            };

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage putRequest()
        {
            string payload = "{\"name\": \"string\", \"description\": \"string\", \"hidden\": true, \"session_id\": \"string\", \"group_id\": \"string\"}";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent("", Encoding.UTF8, "application/json"),
                Method = HttpMethod.Put,
                RequestUri = new Uri("mission_scheduler")
            };

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest()
        {
            string payload = "{\r\n  \"mission_id\": \"" + guid + "\"\r\n}";

            logger(AREA, DEBUG, payload);

            //string payload = "{\r\n  \"mission_id\": \"a5e518af-820d-11e9-8328-0000000000" + stringyfyMission(missionNumber) + "\"\r\n}";

            Uri uri = new Uri("http://" + fleetManagerIP + "/api/v2.0.0/mission_scheduler");

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = uri
                //RequestUri = new Uri("mission_scheduler")
            };

            logger(AREA, DEBUG, "Request Created");

            return request;
        }

        public HttpRequestMessage postRequest(string guid, string name, string description, string hidden, string group_id, string created_by)
        {
            // TODO: change the session ID to be dynamic
            string payload;
            payload = "{\"guid\": \"" + guid + "\", ";
            payload += "\"name\": \"" + name + "\", ";
            payload += "\"description\": \"" + description + "\", ";
            payload += "\"hidden:\": " + hidden + ", ";
            payload += "\"group_id\": \"" + group_id + "\", ";
            payload += "\"session_id\": \"7198db1f - 0474 - 11ea - 84a7 - 0001298f8a0a\", ";
            payload += "\"created_by\": \"" + created_by + "\"}";

            logger(AREA, DEBUG, payload);

            Uri uri = new Uri("http://" + fleetManagerIP + "/api/v2.0.0/missions");

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = uri
            };

            logger(AREA, DEBUG, "Request Created");

            return request;
        }

        public HttpRequestMessage postRequest(bool newMissionCreation)
        {
            // TODO: change the session ID to be dynamic
            string payload;
            payload = "{\"guid\": \"" + guid + "\", ";
            payload += "\"name\": \"" + name + "\", ";
            payload += "\"description\": \"" + description + "\", ";
            payload += "\"hidden:\": " + hidden + ", ";
            payload += "\"group_id\": \"" + group_id + "\", ";
            payload += "\"session_id\": \"7198db1f-0474-11ea-84a7-0001298f8a0a\", ";
            payload += "\"created_by\": \"" + created_by_id + "\"}";

            logger(AREA, DEBUG, payload);

            Uri uri = new Uri("http://" + fleetManagerIP + "/api/v2.0.0/missions");

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = uri
            };

            logger(AREA, DEBUG, "Request Created");

            return request;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public HttpRequestMessage postRequest(int robotID)
        {
            string payload;
            payload = "{\"mission_id\": \"" + guid + "\", ";
            payload += "\"robot_id\": " + robotID + "} ";

            if(robotID == fleetID)
            {
                // Send available mission
                /* 
                string payload = "{\r\n  \"mission_id\": \"" + guid + "\"\r\n}";

                           if(fleet == fleetID)
                            {
                                payload = "{\r\n  \"mission_id\": \"" + guid + "\"\r
                                payload += \n}";
                            }*/
            }


            logger(AREA, DEBUG, payload);


            logger(AREA, DEBUG, payload);

            Uri uri = new Uri("http://" + fleetManagerIP + "/api/v2.0.0/mission_scheduler");

            //string payload = "{\r\n  \"mission_id\": \"a5e518af-820d-11e9-8328-0000000000" + stringyfyMission(missionNumber) + "\"\r\n}";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = uri
                //RequestUri = new Uri("mission_scheduler")
            };

            logger(AREA, DEBUG, "Request Created");

            return request;
        }

    }
}
