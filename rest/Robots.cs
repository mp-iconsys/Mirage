using System;
using System.Collections.Generic;
using System.Text;

namespace Mirage.rest
{
    public class Robots
    {
            /// <summary>
            /// 
            /// </summary>
            public string active { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public List<string> allowed_methods { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string created_by { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string created_by_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string created_by_name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string description { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int fleet_state { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string fleet_state_text { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string ip { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int robot_group_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string robot_model { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string serial_number { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public Status status { get; set; }

        public Robots()
        {

        }

        public class ErrorsItem
        {
            public int code { get; set; }
            public string description { get; set; }
            public string module { get; set; }
        }

        public class Trolley
        {
            /// <summary>
            /// 
            /// </summary>
            public float height { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float length { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float offset_locked_wheels { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float width { get; set; }
        }

        public class Hook_status
        {
            /// <summary>
            /// 
            /// </summary>
            public float angle { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string available { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string braked { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float height { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float length { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public Trolley trolley { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string trolley_attached { get; set; }
        }

        public class Position
        {
            /// <summary>
            /// 
            /// </summary>
            public double orientation { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public double x { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public double y { get; set; }
        }

        public class OptionsItem
        {
        }

        public class Timeout
        {
            /// <summary>
            /// 
            /// </summary>
            public int nsecs { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int secs { get; set; }
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
            public string has_request { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public List<OptionsItem> options { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string question { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public Timeout timeout { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string user_group { get; set; }
        }

        public class Velocity
        {
            /// <summary>
            /// 
            /// </summary>
            public float angular { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float linear { get; set; }
        }

        public class Status
        {
            /// <summary>
            /// 
            /// </summary>
            public float battery_percentage { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int battery_time_remaining { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float battery_voltage { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public float distance_to_next_target { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public List<ErrorsItem> errors { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string footprint { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public Hook_status hook_status { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string joystick_low_speed_mode_enabled { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string joystick_web_session_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string map_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int mission_queue_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string mission_text { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int mode_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string mode_key_state { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string mode_text { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public double moved { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public Position position { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string robot_name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string safety_system_muted { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string session_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string software_version { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int state_id { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string state_text { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string unloaded_map_changes { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public int uptime { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public User_prompt user_prompt { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public Velocity velocity { get; set; }
        }
    }
}
