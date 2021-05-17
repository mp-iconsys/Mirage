using MySql.Data.MySqlClient;
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

        //=========================================================|
        // TODO: fetch these from the database                     |
        //=========================================================|
        public int alarmOffset = 424;
        public int alarmBlockSize = 22;
        public Alarm[] alarm_array = new Alarm[176];

        public int conveyorOffset = 460;
        public int conveyorBlockSize = 1;
        public Alarm[] conveyor_array = new Alarm[9];

        public Alarms()
        {
            string[] alarm_names = { 
"AV010 North", 
"AV010 South",
"AV020 North",
"AV020 South",
"AV030 North",
"AV030 South",
"AV040 North",
"AV040 South",
"AV040 West",
"AV010 East",
"10",
"11",
"12",
"13",
"14",
"15",

"UKL South", 
"D7a South",
"HTR2 South",
"HTR3 South",
"MLA South",
"HTR4 South",
"HTR7 South",
"HTR9 South",
"UKL West",
"D7a West",
"HTR1 West",
"HTR3 West",
"MLA West",
"HTR4 West",
"HTR7 West",
"HTR9 West",

"UKL East",
"D7a East",
"HTR2 East",
"HTR3 East",
"MLA East",
"HTR4 East",
"HTR7 East",
"HTR9 East",
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
"HTR2",
"HTR3",
"MLA a",
"HTR4",
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
"HTR2",
"HTR3",
"MLA a",
"HTR4",
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
"HTR2_In",
"HTR3_In",
"MLA_In",
"HTR4_In",
"HTR7_In",
"HTR9_In",
"UKL_Out",
"D7a_Out",
"HTR1_Out",
"HTR3_Out",
"MLA_Out",
"HTR4_Out",
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

"Delivery to UKL",
"Delivery to D7a",
"Delivery to HTR2",
"Delivery to HTR3",
"Delivery to MLA",
"Delivery to HTR4",
"Delivery to HTR7",
"Delivery to HTR9",
"Collection From UKL",
"Collection From D7a",
"Collection From HTR2",
"Collection From HTR3",
"Collection From MLA",
"Collection From HTR4",
"Collection From HTR7",
"Collection From HTR9",

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


            string[] conveyorName = { "UKL", "D7a", "HTR2", "HTR3", "MLA", "HTR4", "HTR7", "HTR9", "A Sequence Is Inhibited" };

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
                    alarm_array[i].area = "Spare";
                }
                else if (i < 176)
                {
                    alarm_array[i].area = "Sequence Errors";
                }
            }
       
            for(int c = 0; c < 8; c++)
            {
                conveyor_array[c] = new Alarm();
                conveyor_array[c].old_triggered = true;
                conveyor_array[c].triggered = true;
                conveyor_array[c].id = c;
                conveyor_array[c].area = conveyorName[c];
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
            if (SiemensPLC.plcConnected)
            {
                for (int i = 0; i < alarmBlockSize * 8; i++)
                {
                    if (alarm_array[i].triggered && !alarm_array[i].old_triggered)
                    {
                        // Rising Edge on the alarm
                        logger(AREA, INFO, alarm_array[i].area + " - " + alarm_array[i].name + " Rising Edge");
                        alarm_array[i].old_triggered = true;
                        alarm_array[i].saveRisingEdgeToDB();
                    }
                    else if (!alarm_array[i].triggered && alarm_array[i].old_triggered)
                    {
                        // Falling Edge on the alarm
                        logger(AREA, INFO, alarm_array[i].area + " - " + alarm_array[i].name + " Falling Edge");
                        alarm_array[i].old_triggered = false;
                        alarm_array[i].saveFallingEdgeToDB();
                        alarm_array[i].id = 0;
                    }
                }
            }
        }

        public void checkConveyorStatus()
        {
            if (SiemensPLC.plcConnected)
            {
                for (int i = 0; i < conveyorBlockSize * 9; i++)
                {
                    if (conveyor_array[i].triggered && !conveyor_array[i].old_triggered)
                    {
                        // Conveyor has returned to health
                        conveyor_array[i].name = "Conveyor Returned To Health";
                        logger(AREA, INFO, conveyor_array[i].area + " - " + conveyor_array[i].name + " Rising Edge");
                        conveyor_array[i].old_triggered = true;
                        conveyor_array[i].updateConveyorStatus(1);
                    }
                    else if (!conveyor_array[i].triggered && conveyor_array[i].old_triggered)
                    {
                        // Conveyor has encountered a fault
                        conveyor_array[i].name = "Conveyor Encountered A Fault";
                        logger(AREA, INFO, conveyor_array[i].area + " - " + conveyor_array[i].name + " Falling Edge");
                        conveyor_array[i].old_triggered = false;
                        conveyor_array[i].updateConveyorStatus(0);
                    }
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

            public void updateConveyorStatus(int status)
            {
                MySqlCommand cmd = new MySqlCommand("update_conveyor_status");

                try
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new MySqlParameter("ID", id));
                    cmd.Parameters.Add(new MySqlParameter("NAME", area));
                    cmd.Parameters.Add(new MySqlParameter("STATUS", status));
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
