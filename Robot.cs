using System;
using System.Collections.Generic;

namespace Mirage
{
    /* Robot Class
     * Contains:
     * - Comms, based on HttpClient class to fetch REST Api data
     * - Generic data about the status of the robot
     * 
     * 
    */

    public class Robot
    {
        // Comms Class. Contains the comms information

        // Registers Class. Contains all 200 PLC registers
        public Registers r = new Registers();

        // Robot status. Contains most of the entries from the /status/ API
        public Status s = new Status();
    }

    public class Status
    {
        public int mode_id { get; set; }
        public int state_id { get; set; }
        public int uptime { get; set; }
        public int battery_time_remaining { get; set; } // In seconds
        public float battery_percentage { get; set; }
        public float distance_to_next_target { get; set; }
        public double moved { get; set; }
        public string allowed_methods { get; set; }
        public string footprint { get; set; }
        public string joystick_low_speed_mode_enabled { get; set; }
        public string joystick_web_session_id { get; set; }
        public string map_id { get; set; }
        public string mission_queue_id { get; set; }
        public string mission_queue_url { get; set; }
        public string mission_text { get; set; }
        public string mode_key_state { get; set; }
        public string mode_text { get; set; }
        public string robot_model { get; set; }
        public string robot_name { get; set; }
        public string safety_system_muted { get; set; }
        public string serial_number { get; set; }
        public string session_id { get; set; }
        public string state_text { get; set; }
        public string unloaded_map_changes { get; set; }

        public Position position { get; set; }
        public List<ErrorsItem> errors { get; set; }
        public User_prompt user_prompt { get; set; }
        public Velocity velocity { get; set; }

        public Status() { }

        public void printStatus()
        {
            Console.WriteLine("mode_id: " + mode_id);
            Console.WriteLine("state_id: " + state_id);
            Console.WriteLine("uptime: " + uptime);
            Console.WriteLine("battery_time_remaining: " + battery_time_remaining);
            Console.WriteLine("battery_percentage: " + battery_percentage);
            Console.WriteLine("distance_to_next_target: " + distance_to_next_target);
            Console.WriteLine("allowed_methods: " + allowed_methods);
            Console.WriteLine("footprint: " + footprint);
            Console.WriteLine("joystick_low_speed_mode_enabled: " + joystick_low_speed_mode_enabled);
            Console.WriteLine("joystick_web_session_id: " + joystick_web_session_id);
        }

    }

    public class Position
    {
        /// <summary>
        /// 
        /// </summary>
        public double y { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double x { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double orientation { get; set; }
    }

    public class ErrorsItem
    {
        /// <summary>
        /// 
        /// </summary>
        public int code { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string module { get; set; }
    }

    public class Velocity
    {
        /// <summary>
        /// 
        /// </summary>
        public double linear { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public double angular { get; set; }
    }

    public class User_prompt
    {
        /// <summary>
        /// 
        /// </summary>
        public string guid { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string question { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public List<string> options { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int timeout { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string user_group { get; set; }
    }

    public class Cart
    {
        /// <summary>
        /// 
        /// </summary>
        public int width { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int length { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int offset_locked_wheels { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int id { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int height { get; set; }
    }

    public class Hook_status
    {
        /// <summary>
        /// 
        /// </summary>
        public string available { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int angle { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public Cart cart { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int height { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string braked { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public int length { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string cart_attached { get; set; }
    }
}
