using System;
using System.Data;
using System.Text;
using System.Net.Http;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.rest
{
    public class Mission : IRest
    {
        public int missionNumber;
        public string guid = "a5e518af - 820d - 11e9 - 8328 - 0000000000"; // Add mission number to that
        public string name;
        public string url;
        public string uri_suffix = "missions";
        public string missionNumberString;

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
            logger(AREA, INFO, "");
            logger(AREA, INFO, "Mission No: " + missionNumber);
            logger(AREA, INFO, "GUID: " + guid);
            logger(AREA, INFO, "Name: " + name);
            logger(AREA, INFO, "Url: " + url);
            logger(AREA, INFO, "URI Suffix: " + uri_suffix);
            logger(AREA, INFO, "missionNumberString: " + missionNumberString);
            logger(AREA, INFO, "");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void saveToMemory(HttpResponseMessage response)
        {

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
                Method = HttpMethod.Delete,
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
            string payload = "{\r\n  \"mission_id\": \"a5e518af-820d-11e9-8328-0000000000" + stringyfyMission(missionNumber) + "\"\r\n}";

            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri("mission_scheduler")
            };

            return request;
        }
    }
}
