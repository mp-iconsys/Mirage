using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

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

        public Mission() { }

        public Mission(int missionNumber)
        {
            this.missionNumber = missionNumber;

            if (missionNumber < 10)
                this.missionNumberString = "0" + missionNumber;
            else
                this.missionNumberString = missionNumber.ToString();

            guid = guid + missionNumberString;
        }

        public string stringyfyMission(int missionNumber)
        {
            if (missionNumber < 10)
                return "0" + missionNumber;
            else
                return missionNumber.ToString();
        }

        public void print()
        {
            Console.WriteLine("Mission No: " + missionNumber);
            Console.WriteLine("GUID: " + guid);
            Console.WriteLine("Name: " + name);
            Console.WriteLine("Url: " + url);
            Console.WriteLine("URI Suffix: " + uri_suffix);
            Console.WriteLine("missionNumberString: " + missionNumberString);
        }

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

            string  query = "REPLACE INTO missions (`MISSION_ID`, `ROBOT_ID`, `GUID`, `Name`, `URL`) VALUES ";
                    query += "('" + missionNumber + "','" + robotID + "','" + guid + "','" + name + "','" + url + "');";

            Globals.issueInsertQuery(query);
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

        public void saveAll(HttpResponseMessage response, int robotID)
        {

        }

        // Create mission
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
