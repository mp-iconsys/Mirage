using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Mirage.rest
{
    public class Mission : IRest
    {
        private int missionNumber;
        private string guid = "a5e518af - 820d - 11e9 - 8328 - 0000000000"; // Add mission number to that
        private string uri_suffix = "missions";
        private string missionNumberString;

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

        public string getPayload(int missionNumber)
        {
            string payload = "{\r\n  \"guid\": \"" + guid + stringyfyMission(missionNumber) + "\",\r\n  \"name\": \"Mission" + stringyfyMission(missionNumber) + "\",\r\n  \"description\": \"template_mission_" + stringyfyMission(missionNumber) + "\",\r\n  \"hidden\": false,\r\n  \"group_id\": \"mirconst-guid-0000-0011-missiongroup\",\r\n  \"session_id\": \"caa94ad9-65cc-11e9-abc1-94c691a7361d\",\r\n  \"created_by_id\": \"mirconst-guid-0000-0004-users0000000\"\r\n}";
            return payload;
        }

        public void print()
        {

        }

        public void saveToMemory(HttpResponseMessage response)
        {

        }

        public void saveToDB(int robotID)
        {

        }

        public void saveAll(HttpResponseMessage response, int robotID)
        {

        }

        // Create mission
        public HttpRequestMessage createMission(int missionNumber)
        {
            HttpRequestMessage request = new HttpRequestMessage
            {
                Content = new StringContent(getPayload(missionNumber), Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
                RequestUri = new Uri("mission_scheduler")
            };

            return request;
        }

        // Clear schedule
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

        // Send mission
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
