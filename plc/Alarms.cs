﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.plc
{
    public class Alarms
    {
        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Alarms);

        public int alarmOffset = 424;
        public int alarmBlockSize = 22;
        public Alarm[] alarm_array = new Alarm[176];

        public Alarms()
        {
            string[] alarm_names = { "AV010_North", "AV010_South","AV020_North","AV020_South","AV030_North","AV030_South","AV040_North","AV040_South","AV040_East","9","10","11","12","13","14","15",
"UKL_South", "D7a_South",
"HTR1_South",
"HTR3_South",
"MLA a_South",
"MLA b _South",
"HTR7_South",
"HTR9_South",
"UKL_West",
"D7a_West",
"HTR1_West",
"HTR3_West",
"MLA a_West",
"MLA b _West",
"HTR7_West",
"HTR9_West",
"UKL_East",
"D7a_East",
"HTR1_East",
"HTR3_East",
"MLA a_East",
"MLA b _East",
"HTR7_East",
"HTR9_East",
"8",
"9",
"10",
"11",
"12",
"13",
"14",
"15",
"AIV",
"UKL",
"D7a",
"HTR1",
"HTR3",
"MLA a",
"MLA b",
"HTR7",
"HTR9",
"9",
"10",
"11",
"12",
"13",
"14",
"15",
"AV010",
"AV020",
"AV030",
"AV040",
"AV050",
"UKL",
"D7a",
"HTR1",
"HTR3",
"MLA a",
"MLA b",
"HTR7",
"HTR9",
"14",
"15",
"16",
"AIV_1",
"AIV_2",
"AIV_3",
"AIV_4",
"AIV_5",
"AIV_6",
"AIV_7",
"AIV_8",
"8",
"9",
"10",
"11",
"12",
"13",
"14",
"15",
"UKL_In",
"D7a_In",
"HTR1_In",
"HTR3_In",
"MLA a_In",
"MLA b_In",
"HTR7_In",
"HTR9_In",
"UKL_Out",
"D7a_Out",
"HTR1_Out",
"HTR3_Out",
"MLA a_Out",
"MLA b_Out",
"HTR7_Out",
"HTR9_Out",
"0",
"1",
"2",
"3",
"4",
"5",
"6",
"7",
"8",
"9",
"10",
"11",
"12",
"13",
"14",
"15",
"AIV_1",
"AIV_2",
"AIV_3",
"AIV_4",
"AIV_5",
"AIV_6",
"AIV_7",
"AIV_8",
"8",
"9",
"10",
"11",
"12",
"13",
"14",
"15",
"UKL_In",
"D7a_In",
"HTR1_In",
"HTR3_In",
"MLA a_In",
"MLA b_In",
"HTR7_In",
"HTR9_In",
"UKL_Out",
"D7a_Out",
"HTR1_Out",
"HTR3_Out",
"MLA a_Out",
"MLA b_Out",
"HTR7_Out",
"HTR9_Out",
"0",
"1",
"2",
"3",
"4",
"5",
"6",
"7",
"8",
"9",
"10",
"11",
"12",
"13",
"14",
"15" };

            int x = 0;

            for (x = 0; x < 176; x++)
            {
                alarm_array[x] = new Alarm();
            }

            int i = 0;

            for (i = 0; i < 176; i++)
            {
                alarm_array[i].name = alarm_names[i];

                if (i < 48)
                {
                    alarm_array[i].area = "EM Stop";
                }
                else if (i < 64)
                {
                    alarm_array[i].area = "Safety Tripped";
                }
                else if (i < 80)
                {
                    alarm_array[i].area = "24V Failure";
                }
                else if (i < 128)
                {
                    alarm_array[i].area = "ToteJammed";
                }
                else if (i < 176)
                {
                    alarm_array[i].area = "Position Error";
                }
            }
        }

        public void printAllAlarms()
        {
            for (int i = 0; i < alarmBlockSize * 8; i++)
            {
                alarm_array[i].print();
            }
        }

        public void checkAlarms()
        {
            for (int i = 0; i < alarmBlockSize * 8; i++)
            {
                if(alarm_array[i].triggered && !alarm_array[i].old_triggered)
                {
                    // Rising Edge on the alarm
                    logger(AREA, INFO, alarm_array[i].area + " -- " + alarm_array[i].name + " Rising Edge");
                    alarm_array[i].old_triggered = true;
                    alarm_array[i].saveRisingEdgeToDB();
                }
                else if(!alarm_array[i].triggered && alarm_array[i].old_triggered)
                {
                    // Falling Edge on the alarm
                    logger(AREA, INFO, alarm_array[i].area + " -- " + alarm_array[i].name + " Falling Edge");
                    alarm_array[i].old_triggered = false;
                    alarm_array[i].saveFallingEdgeToDB();
                    alarm_array[i].id = 0;
                }
            }
        }

        public class Alarm
        {
            public int id { get; set; }
            public bool old_triggered { get; set; }
            public bool triggered { get; set; }
            public string name { get; set; }
            public string area { get; set; }

            public Alarm()
            {
                id = 0;
                old_triggered = false;
                triggered = false;
                name = "";
                area = "";
            }

            public void print()
            {
                if (triggered)
                {
                    logger(AREA, INFO, area + " -- " + name + " Alarm Is Active");
                }
            }

            public void saveRisingEdgeToDB()
            {
                //MySqlCommand cmd = new MySqlCommand("rising_edge_alarm");

                try
                {
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = db;
                    cmd.CommandText = "rising_edge_alarm";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ALARM_AREA", area);
                    cmd.Parameters["@ALARM_AREA"].Direction = ParameterDirection.Input;

                    cmd.Parameters.AddWithValue("@ALARM_TEXT", name);
                    cmd.Parameters["@ALARM_TEXT"].Direction = ParameterDirection.Input;

                    cmd.Parameters.Add("@LID", MySqlDbType.Int32);
                    cmd.Parameters["@LID"].Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();
                    id = (int)cmd.Parameters["@LID"].Value;
                    cmd.Dispose();
                }
                catch (Exception exception)
                {
                    logger(AREA, ERROR, "MySQL Query Error: ", exception);
                }
            }

            public void saveFallingEdgeToDB()
            {
                MySqlCommand cmd = new MySqlCommand("falling_edge_alarm");

                try
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new MySqlParameter("LID", id));
                    issueQuery(cmd);
                }
                catch (Exception exception)
                {
                    cmd.Dispose();
                    logger(AREA, ERROR, "MySQL Query Error: ", exception);
                }
            }

        }
    }
}