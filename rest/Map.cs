using System;
using System.Data;
using System.Text;
using System.Net.Http;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;

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

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Map);

        /// <summary>
        /// Prints map data from memory to the log file and console.
        /// </summary>
        public void print()
        {
            logger(AREA, INFO, "");
            logger(AREA, INFO, "==== Printing Map: " + Map_id + " ====");
            logger(AREA, INFO, "Created_by: " + Created_by);
            logger(AREA, INFO, "Created_by_id: " + Created_by_id);
            logger(AREA, INFO, "Created_by_name: " + Created_by_name);
            logger(AREA, INFO, "Guid: " + Guid);
            logger(AREA, INFO, "map: " + map);
            logger(AREA, INFO, "Metadata: " + Metadata);
            logger(AREA, INFO, "Name: " + Name);
            logger(AREA, INFO, "One_way_map: " + One_way_map);
            logger(AREA, INFO, "Origin_theta: " + Origin_theta);
            logger(AREA, INFO, "Origin_x: " + Origin_x);
            logger(AREA, INFO, "Origin_y: " + Origin_y);
            logger(AREA, INFO, "Path_guides: " + Path_guides);
            logger(AREA, INFO, "Paths: " + Paths);
            logger(AREA, INFO, "Positions: " + Positions);
            logger(AREA, INFO, "Resolution: " + Resolution);
            logger(AREA, INFO, "Session_id: " + Session_id);
            logger(AREA, INFO, "Url: " + Url);
            logger(AREA, INFO, "==== Finished Printing Map: " + Map_id + " ====");
            logger(AREA, INFO, "");

/*            Console.WriteLine();
            Console.WriteLine("==== PRINTING MAP NO: " + Map_id + " ====");
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
            Console.WriteLine();*/
        }

        /// <summary>
        /// Saves map data to memory, based on the HTTP Response data.
        /// </summary>
        /// <param name="response"></param>
        public void saveToMemory(HttpResponseMessage response) {}

        /// <summary>
        /// Saves map to the database.
        /// </summary>
        /// <param name="robotID"></param>
        public void saveToDB(int robotID)
        {
            MySqlCommand cmd = new MySqlCommand("store_maps");

            try
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new MySqlParameter("MAP_ID", Map_id));
                cmd.Parameters.Add(new MySqlParameter("ROBOT_ID", robotID));
                cmd.Parameters.Add(new MySqlParameter("NAME", Name));
                cmd.Parameters.Add(new MySqlParameter("GUID", Guid));
                cmd.Parameters.Add(new MySqlParameter("CREATED_BY_NAME", Created_by_name));
                cmd.Parameters.Add(new MySqlParameter("CREATED_BY_ID", Created_by_id));
                cmd.Parameters.Add(new MySqlParameter("MAP", map));
                cmd.Parameters.Add(new MySqlParameter("METADATA", Metadata));
                cmd.Parameters.Add(new MySqlParameter("ONE_WAY_MAP", One_way_map));
                cmd.Parameters.Add(new MySqlParameter("ORIGIN_THETA", Origin_theta));
                cmd.Parameters.Add(new MySqlParameter("ORIGIN_X", Origin_x));
                cmd.Parameters.Add(new MySqlParameter("ORIGIN_Y", Origin_y));
                cmd.Parameters.Add(new MySqlParameter("PATH_GUIDES", Path_guides));
                cmd.Parameters.Add(new MySqlParameter("PATHS", Paths));
                cmd.Parameters.Add(new MySqlParameter("POSITIONS", Positions));
                cmd.Parameters.Add(new MySqlParameter("RESOLUTION", Resolution));

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
                RequestUri = new Uri("maps/" + Guid)
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
