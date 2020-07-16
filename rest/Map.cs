using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Mirage.rest
{
    public class Map : IRest
    {
        public List<string> Allowed_methods { get; set; }
        public string Created_by { get; set; }
        public string Created_by_id { get; set; }
        public string Created_by_name { get; set; }
        public string Guid { get; set; }
        public string map { get; set; }
        public string Metadata { get; set; }
        public string Name { get; set; }
        public string One_way_map { get; set; }
        public float Origin_theta { get; set; }
        public float Origin_x { get; set; }
        public float Origin_y { get; set; }
        public string Path_guides { get; set; }
        public string Paths { get; set; }
        public string Positions { get; set; }
        public double Resolution { get; set; }
        public string Session_id { get; set; }
        public string Url { get; set; }
        public int Map_id { get; set; }

        public void print()
        {
            Console.WriteLine();
            Console.WriteLine("==== MAP NO: " + Map_id + " ====");
            Console.WriteLine("Created_by: " + Created_by);
            Console.WriteLine("Created_by_id: " + Created_by_id);
            Console.WriteLine("Created_by_name: " + Created_by_name);
            Console.WriteLine("Guid: " + Guid);
            Console.WriteLine("map: " + map);
            Console.WriteLine("Metadata: " + Metadata);
            Console.WriteLine("Name: " + Name);
            Console.WriteLine("One_way_map: " + One_way_map);
            Console.WriteLine("Origin_theta: " + Origin_theta);
            Console.WriteLine("Origin_x: " + Origin_x);
            Console.WriteLine("Origin_y: " + Origin_y);
            Console.WriteLine("Path_guides: " + Path_guides);
            Console.WriteLine("Paths: " + Paths);
            Console.WriteLine("Positions: " + Positions);
            Console.WriteLine("Resolution: " + Resolution);
            Console.WriteLine("Session_id: " + Session_id);
            Console.WriteLine("Url: " + Url);
            Console.WriteLine("==== END OF MAP PRINT NO: " + Map_id + " ====");
            Console.WriteLine();
        }

        public void saveToMemory(HttpResponseMessage response) {}

        public void saveToDB() {}

        public void saveToDB(int robotID)
        {
            string query = "REPLACE INTO maps (`MAP_ID`, `ROBOT_ID`, `NAME`, `CREATED_BY_NAME`, `CREATED_BY_ID`, `MAP`, `METADATA`, `ONE_WAY_MAP`, `ORIGIN_THETA`, `ORIGIN_X`, `ORIGIN_Y`, `PATH_GUIDES`, `PATHS`, `POSITIONS`, `RESOLUTION`) VALUES ";

            query   += "('" + Map_id + "', '" + robotID + "', '" + Name + "', '" + Created_by_name + "', '" + Created_by_id + "', '" + map + "', '" + Metadata + "', '"
                    + One_way_map + "', '" + Origin_theta + "', '" + Origin_x + "', '" + Origin_y + "', '" + Path_guides + "', '" + Paths + "', '"
                    + Positions + "', '" + Resolution + "');";

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
                RequestUri = new Uri("maps/" + Guid)
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
                RequestUri = new Uri("mission_scheduler")
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
