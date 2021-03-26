using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using log4net;
using log4net.Config;
using System.IO;
using Mirage;
using System.Collections.Generic;
using static Globals.DebugLevel;
using Mirage.Reporting;
using System.Text;
using System.Threading;

public static class Globals
{
    //=========================================================|
    //  Global Variables                                       |     
    //=========================================================|
    public static bool keepRunning = true;
    public static bool resumingSession = false;
    public static int pollInterval;
    public static int sizeOfFleet;
    public static MySqlConnection db;
    public static HttpClient comms;
    public static AuthenticationHeaderValue fleetManagerAuthToken;
    public static string fleetManagerIP;
    public static Fleet mirFleet;
    public static Reporting reports = new Reporting();

    //=========================================================|
    //  Helper Variables                                       |
    //=========================================================|
    public const int fleetID = 666;
    public const int PLCMissionOffset = 301;

    //=========================================================|
    //  Used For Logging & Debugging                           |     
    //=========================================================|
    public static int debugLevel = 0;
    private static readonly Type AREA = typeof(Globals);
    public static ILog log = LogManager.GetLogger(AREA);

    //=========================================================|
    //  For Sending SMS Alerts                                 |     
    //=========================================================|
    private static List<string> phone_numbers;
    private static string accountSid;
    private static string authToken;
    private static string phone_twilio;

    /// <summary>
    /// Different Levels For Printing Debug Messages
    /// </summary>
    public enum DebugLevel
    {
        INFO = 1,
        DEBUG = 2,
        WARNING = 3,
        ERROR = 4,
        FATAL = 5,
        ALL = 6
    }

    /// <summary>
    /// Used for issuing tasks to the fleet or robots. Matches PLC Task Codes. Does not conatin any methods.
    /// </summary>
    public static class Tasks
    {
        public const int GetScheduleStatus = 100;
        public const int CreateMission = 101;
        public const int SendMissionToScheduler = 102;
        public const int ClearScheduler = 103;
        public const int GetBattery = 200;
        public const int GetDistance = 201;
        public const int GetRobotStatus = 202;
        public const int SendRobotMission = 208;
        public const int ReleaseRobot = 353;
    }

    /// <summary>
    /// Task Status Codes that match PLC Task Status. Does not conatin any methods.
    /// </summary>
    public static class TaskStatus
    {
        public const int Idle = 0;
        public const int AwaitingPickUp = 10;
        public const int TaskReceivedFromPLC = 10;
        public const int StartedProcessing = 20;
        public const int CompletedNoErrors = 30;
        public const int CompletedPartially = 30;
        public const int FatalError = 40;
        public const int CouldntProcessRequest = 40;

        public const int PlcIdle = 0;
        public const int PlcError = 0;
    }

    /// <summary>
    /// Reads settings from the DB or config files.
    /// The files are used as a backup in case DB is not available.
    /// They are: log4net.config, plc.config and App.config, located in /config
    /// </summary>
    public static void readAllSettings()
    {
        // Begins logging to the console and file
        try
        {
            var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo(@"config\log4net.config"));
        }
        catch (Exception exception)
        {
            Console.WriteLine("Failed To Read Logger Configuration.");
            Console.WriteLine(exception);
        }

        logger(AREA, INFO, "Starting Mirage v0.18");
        logger(AREA, INFO, "Obtaining Settings");

        try
        {
            connectToDB();

            readSettingsFromDB();
        }
        catch (ConfigurationErrorsException exception)
        {
            logger(AREA, DEBUG, "Couldn't Read App Settings. Error: ", exception);
            gracefulTermination("Failed to read application settings, on start-up. App will terminate");
        }

        //=========================================================|
        //  Initialize Siemens PLC                                 |     
        //=========================================================|
        SiemensPLC.initialize();

        //=========================================================|
        //  Initialize SMS communication for alerts                |     
        //=========================================================|
        TwilioClient.Init(accountSid, authToken);

        //=========================================================|
        //  Initialize PLC comms and REST Header                   |     
        //=========================================================|
        setUpDefaultComms();

        //=========================================================|
        //  Initialize Fleet Container                             |     
        //=========================================================|
        if (fleetManagerIP != null && fleetManagerAuthToken != null)
        {
            //sendSMS("Starting AMR-Connect");
            logger(AREA, INFO, "Fleet Data Assigned");
            mirFleet = new Fleet(sizeOfFleet, fleetManagerIP, fleetManagerAuthToken);
        }
        else
        {
            logger(AREA, INFO, "Fleet Data Missing Or Fleet Not Used");
            mirFleet = new Fleet(sizeOfFleet);
        }

        logger(AREA, DEBUG, "Settings Obtained");
    }

    /// <summary>
    /// Establishes a connection to master database. If not available, it switches over to a local slave.
    /// </summary>
    public static void connectToDB()
    {
        logger(AREA, DEBUG, "==== Connecting To Databases ====");

        try
        {
            db = new MySqlConnection(ConfigurationManager.ConnectionStrings["master"].ConnectionString);
            db.Open();

            logger(AREA, INFO, "Connected To Master DB");
        }
        catch (MySqlException exception)
        {
            logger(AREA, ERROR, "Master DB connection failed with error: ", exception);
            logger(AREA, INFO, "Attempting to connect to slave");

            try
            {
                db = new MySqlConnection(ConfigurationManager.ConnectionStrings["slave"].ConnectionString);
                db.Open();

                logger(AREA, INFO, "Connected To Slave DB");
                sendSMS("Failed to connect to master database.");
            }
            catch (MySqlException e)
            {
                logger(AREA, ERROR, "Local connection failed with error: ", e);
                sendSMS("Failed to connect to both master and slave databases. Check if they're up and if the network is blocked.");
            }
        }
    }

    /// <summary>
    /// Obtains application, phone numbers and fleet manager settings from the Database.
    /// </summary>
    public static void readSettingsFromDB()
    {
        //=========================================================|
        //  Read App Configuration                                 |     
        //=========================================================|
        try
        { 
            string sql = "SELECT * FROM app_config;";
            using var cmd = new MySqlCommand(sql, db);
            using MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - Variable: " + rdr.GetString(1) + " - Value: " + rdr.GetString(2));

                if(rdr.GetString(1) == "pollingInterval")
                {
                    pollInterval = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "debugLevel")
                {
                    debugLevel = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "sizeOfFleet")
                {
                    sizeOfFleet = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "resumingSession")
                {
                    resumingSession = Boolean.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "twilioSid")
                {
                    accountSid = rdr.GetString(2);
                }
                else if (rdr.GetString(1) == "twilioAuthToken")
                {
                    authToken = rdr.GetString(2);
                }
                else if (rdr.GetString(1) == "twilioPhone")
                {
                    phone_twilio = rdr.GetString(2);
                }
                else if (rdr.GetString(1) == "fleetManagerIP")
                {
                    fleetManagerIP = rdr.GetString(2);
                }
                else if (rdr.GetString(1) == "fleetManagerAuthToken")
                {
                    fleetManagerAuthToken = new AuthenticationHeaderValue("Basic", rdr.GetString(2));
                }
            }
        }
        catch
        {

        }

        //=========================================================|
        //  Read SMS Phone Numbers For Alerts                      |     
        //=========================================================|
        try
        {
            phone_numbers = new List<string>();

            string sql = "SELECT * FROM alert_phone_numbers;";
            using var cmd = new MySqlCommand(sql, db);
            using MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + ", Phone Number: " + rdr.GetString(1));
                phone_numbers.Add(rdr.GetString(1));
            }
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "Couldn't fetch phone numbers. Error is: ", exception);
        }
    }

    /// <summary>
        /// Establishes default communications with the PLC and standard headers for HTTP REST traffic.
        /// </summary>
    public static void setUpDefaultComms()
    {
        logger(AREA, DEBUG, "==== Setting Up Connection Details ====");

        try
        {
            comms = new HttpClient();
            comms.DefaultRequestVersion = HttpVersion.Version11;
            comms.DefaultRequestHeaders.Accept.Clear();
            comms.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            comms.DefaultRequestHeaders.Add("Accept-Language", "en_US");
            comms.Timeout = TimeSpan.FromMinutes(10);

            logger(AREA, INFO, "MiR HTTP Header Created");
        }
        catch(Exception exception)
        {
            logger(AREA, ERROR, "Failed to set up an HTTP Connection. Error: ", exception);
        }

        SiemensPLC.establishConnection();

        logger(AREA, DEBUG, "==== Connections Established ====");
    }

    /// <summary>
    /// Disposes default communication: HTTP Client as well as Siemens Libnodave connection.
    /// </summary>
    public static void closeComms()
    {
        logger(AREA, DEBUG, "==== Closing Connections ====");

        try
        {
            comms.Dispose();
            SiemensPLC.disconnect();

            logger(AREA, INFO, "Closed Communications");
        }
        catch(NullReferenceException exception)
        {
            logger(AREA, ERROR, "Couldn't close comms as they've not been instantiated: ", exception);
        }
        catch(Exception exception)
        {
            logger(AREA, ERROR, "Couldn't close comms because of the following exception: ", exception);
        }

        logger(AREA, DEBUG, "==== Finished Closing Connections ====");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmd"></param>
    public static void issueQuery(MySqlCommand cmd)
    {
        logger(AREA, DEBUG, "==== Closing Connections ====");

        int rowsAffected = 0;

        try
        {
            cmd.Connection = db;
            cmd.Prepare();
            rowsAffected = cmd.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                // Query Worked (for transactions and procedures)
                logger(AREA, DEBUG, "MySQL Transaction Was Successful");
            }

            cmd.Dispose();
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "MySQL Query Failed with error: ", exception);
        }

        logger(AREA, DEBUG, "==== Closing Connections ====");
    }

    /// <summary>
    /// Checks if configuration has been changed on the database and re-implements the app parameters.
    /// </summary>
    public static void checkConfigChanges()
    {
        try
        {
            bool updateConfigs = false;

            string sql = "SELECT process FROM app_update;";
            using var cmd = new MySqlCommand(sql, db);
            using MySqlDataReader rdr = cmd.ExecuteReader();

            while(rdr.Read())
            {
                if(rdr.GetInt16(0) == 0)
                {
                    updateConfigs = true;
                }
                else if(rdr.GetInt16(0) == 1)
                {
                    updateConfigs = false;
                }
                else
                {

                }
            }

            if(updateConfigs)
            {
                logger(AREA, INFO, "==== Updating Runtime Parameters From DB ====");

                try 
                {
                    cmd.Dispose();
                    rdr.Close();
                }
                catch
                {

                }

                readSettingsFromDB();

                logger(AREA, DEBUG, "==== Reset Updates ====");

                using var cmd1 = new MySqlCommand("UPDATE app_update SET PROCESS = 1;", db);
                issueQuery(cmd1);
            }
        }
        catch(Exception e)
        {
            logger(AREA, ERROR, "Failed Runtime Parameter Update Check");
        }
    }

    /// <summary>
    /// Checks if plc wants to reset has been changed on the database and re-implements the app parameters.
    /// </summary>
    public static void checkPLCReset()
    {
        try
        {
            bool plcResetRequired = false;

            string sql = "SELECT reset_required FROM plc_reset;";
            using var cmd = new MySqlCommand(sql, db);
            using MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                if (rdr.GetInt16(0) == 1)
                {
                    plcResetRequired = true;
                }
            }

            if (plcResetRequired)
            {
                logger(AREA, INFO, "Issuing A Sequence Reset From The PLC");
                logger(AREA, INFO, "Getting More Data");

                try
                {
                    cmd.Dispose();
                    rdr.Close();
                }
                catch
                {
                    logger(AREA, ERROR, "Failed To Dispose MySQL Resource");
                }

                readPLCSequenceBreak();

                logger(AREA, DEBUG, "==== Reset Updates ====");

                using var cmd1 = new MySqlCommand("UPDATE app_update SET PROCESS = 0;", db);
                issueQuery(cmd1);
            }
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "Failed Runtime Parameter Update Check");
        }
    }

    public static void readPLCSequenceBreak()
    {
        try
        {
            string sql = "SELECT * FROM plc_sequence_reset WHERE RESET = 1;";
            using var cmd = new MySqlCommand(sql, db);
            using MySqlDataReader rdr = cmd.ExecuteReader();
            int id = 999;

            while (rdr.Read())
            {
                id = rdr.GetInt32(0);
                SiemensPLC.updateResetBits(id);
            }

            cmd.Dispose();
            rdr.Close();

            if (id < 999)
            { 
                using var cmd1 = new MySqlCommand("UPDATE plc_sequence_reset SET RESET = 0 WHERE ID = " + id + ";", db);
                issueQuery(cmd1);

                cmd1.Dispose();
                rdr.Close();

                using var cmd2 = new MySqlCommand("UPDATE plc_reset SET RESET_REQUIRED = 0", db);
                issueQuery(cmd2);
            }
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "Failed Resetting Sequence Bits");
            logger(AREA, ERROR, "Exception: ", e);
        }
    }


    /// <summary>
    /// Sends an SMS alert and terminates the program. Only used in extreme cases.
    /// </summary>
    /// <param name="terminationReason">Reason for termination and the content of an SMS alert.</param>
    public static void gracefulTermination(string terminationReason)
    {
        logger(AREA, INFO, "==== Terminating Unexpectedly ====");
        logger(AREA, INFO, terminationReason);

        closeComms();

        sendSMS(terminationReason);

        Environment.Exit(1);
    }

    /// <summary>
    /// Sends an SMS alert and terminates the program. Only used in extreme cases.
    /// </summary>
    /// <param name="terminationReason">Reason for termination and the content of an SMS alert.</param>
    public static void gracefulTermination()
    {
        logger(AREA, INFO, "==== Graceful Termination ====");

        closeComms();

        Environment.Exit(1);
    }

    /// <summary>
    /// Sends an SMS Alert 
    /// </summary>
    /// <param name="message"></param>
    public static void sendSMS(string message)
    {
        logger(AREA, DEBUG, "Sending SMS Alert");

        try
        {
            
            for (int i = 0; i < phone_numbers.Count; i++)
            {
                MessageResource.Create(
                   to: new PhoneNumber(phone_numbers[i]),
                   from: new PhoneNumber(phone_twilio),
                   body: message);
            }

            logger(AREA, INFO, "Sending An SMS Alert: " + message);
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "Failed To Send SMS. Error: ", exception);
        }

        logger(AREA, DEBUG, "==== SMS Alert Sent ====");
    }

    /// <summary>
    /// Sends various parameters on start-up so that fleet is correct
    /// </summary>
    public static void initializeFleet()
    {
        logger(AREA, INFO, "Initializing Fleet Data: Robot, Mission and Charging Groups + Missions");

        if(resumingSession)
        {
            logger(AREA, INFO, "Resuming Previous Session, i.e: Using Existing Fleet Set-Up");
        }
        else
        {
            logger(AREA, INFO, "Starting A New Fleet Instance: Deploying Data To Fleet");

            //=========================================================|
            // Post All the Robot Groups                               |
            //=========================================================|
            try
            {
                logger(AREA, INFO, "Initializing Robot Groups");

                string sql = "SELECT * FROM robot_groups;";
                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    Mirage.rest.RobotGroup temp = new Mirage.rest.RobotGroup();

                    string name = rdr.GetString(1);
                    string desc = rdr.GetString(2);
                    string allow_all = rdr.GetString(3);
                    string created_by = rdr.GetString(4);

                    logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - Name: " + name + " - Description: " + desc + "Allow All Mission: " + allow_all + "Created By: " + created_by);

                    //mirFleet.group[i] = new Mirage.rest.RobotGroup();
                    HttpRequestMessage tempReq = temp.postRequest(name, desc, allow_all, created_by);

                    mirFleet.fleetManager.sendRESTdata(tempReq);
                    Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                logger(AREA, DEBUG, "Failed a query: ", e);
            }

            logger(AREA, DEBUG, "Robot Groups Sent To Fleet");

            //=========================================================|
            // Post the Mission Groups                                 |
            //=========================================================|
            try
            {
                logger(AREA, INFO, "Initializing Mission Groups");

                string sql = "SELECT * FROM mission_groups;";
                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    string guid = rdr.GetString(1);
                    string name = rdr.GetString(2);
                    int priority = rdr.GetInt32(3);
                    string feature = rdr.GetString(4);
                    string icon = rdr.GetString(5);
                    string created_by = rdr.GetString(6);

                    logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - GUID: " + guid + " - Name: " + name + "Priority: " + priority + "Feature: " + feature + " icon: " + icon + " Created by: " + created_by);

                    var plainTextBytes = Encoding.UTF8.GetBytes(icon);

                    string payload;
                    payload = "{\"guid\": \"" + guid + "\", ";
                    payload += "\"name\": \"" + name + "\", ";
                    payload += "\"priority\": " + priority + ", ";
                    payload += "\"feature\": \"" + feature + "\", ";
                    payload += "\"icon\": \"" + Convert.ToBase64String(plainTextBytes) + "\"} ";
                    //payload += "\"created_by_id\": \"" + created_by + "\"}";

                    logger(AREA, DEBUG, payload);

                    string url = "http://" + fleetManagerIP + "/api/v2.0.0/mission_groups/";
                    Uri uri = new Uri(url);

                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Content = new StringContent(payload, Encoding.UTF8, "application/json"),
                        Method = HttpMethod.Post,
                        RequestUri = uri
                    };

                    logger(AREA, DEBUG, request.Content.ToString());

                    mirFleet.fleetManager.sendRESTdata(request);

                    Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                logger(AREA, DEBUG, "Failed a query: ", e);
            }

            logger(AREA, DEBUG, "Finished Initializing Mission Groups");
        }

        //=========================================================|
        // Post the Missions                                       |
        //=========================================================|
        try
        {
            logger(AREA, INFO, "Initializing Missions");

            string sql = "SELECT * FROM missions;";
            using var cmd = new MySqlCommand(sql, db);
            using MySqlDataReader rdr = cmd.ExecuteReader();

            int i = 0;

            while (rdr.Read())
            {

                //mirFleet.fleetManager.Missions[i] = new Mirage.rest.Mission();
                string guid = rdr.GetString(2);
                string name = rdr.GetString(3);
                string description = rdr.GetString(4);
                string hidden = rdr.GetString(5);
                string group_id = rdr.GetString(6);
                string created_by = rdr.GetString(7);
                string url_string = rdr.GetString(8);

                mirFleet.fleetManager.Missions[i].missionNumber = i;
                mirFleet.fleetManager.Missions[i].guid = guid;
                mirFleet.fleetManager.Missions[i].name = name;
                mirFleet.fleetManager.Missions[i].description = description;
                mirFleet.fleetManager.Missions[i].hidden = hidden;
                mirFleet.fleetManager.Missions[i].group_id = group_id;
                mirFleet.fleetManager.Missions[i].created_by_id = created_by;
                mirFleet.fleetManager.Missions[i].url_string = url_string;

/*                if(i > 61)
                {
                    mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[i].postRequest(true));
                }*/

                if(resumingSession == false)
                {
                    logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - GUID: " + guid + " - Name: " + name + "Desc: " + description + "Hidden: " + hidden + " group_id: " + group_id + " created_by: " + created_by + " URL: " + url_string);

                    mirFleet.fleetManager.sendRESTdata(mirFleet.fleetManager.Missions[i].postRequest(true));
                }

                Thread.Sleep(50);
                i++;
            }

            logger(AREA, DEBUG, "Finished Initializing Missions");
        }
        catch (Exception e)
        {
            logger(AREA, DEBUG, "Failed a query: ", e);
        }

        mirFleet.getFleetRobotIDs();

        logger(AREA, INFO, "Finished Fleet Initialization");
    }

    /// <summary>
    /// Copies the data from Mirage internal memory to PLC buffer
    /// </summary>
    public static void fleetMemoryToPLC()
    {
        // Copy the internal Fleet Data to PLC fleetBlock
        SiemensPLC.fleetBlock.Param[SiemensPLC.fleetBlockControlParameters+1].setValue(mirFleet.returnParameter);

        int j = 0;
        int robotsCurrentGroup;

        //
        // For each robot, check their robot group. 
        // If it matches the group number, add it in. Otherwise, do nothing.
        //
        for (int group = 0; group < mirFleet.groups.Length; group++)
        {
            mirFleet.groups[group] = 0;

            for (int robotID = 0; robotID < sizeOfFleet; robotID++)
            {
                robotsCurrentGroup = mirFleet.robots[robotID].s.robot_group_id;

                if (robotsCurrentGroup == group)
                {
                    mirFleet.groups[group]++;
                }
            }
        }

        for (int i = SiemensPLC.fleetBlockControlParameters + 2; i < SiemensPLC.fleetBlock.Param.Count; i++)
        {
            logger(AREA, DEBUG, "i: " + i + " - j: " + j + " - End At: " + SiemensPLC.fleetBlock.Param.Count);

            SiemensPLC.fleetBlock.Param[i].setValue(mirFleet.groups[j]);
            SiemensPLC.fleetBlock.Param[i].print();

            j++;
        }

        logger(AREA, DEBUG, "Finished Copying Mirage Internal Data to PLC Buffer");
    }

    /// <summary>
    /// Saves the internal Mirage memory of a single robot to the PLC buffer used for writes and reads
    /// </summary>
    public static void robotMemoryToPLC(int robotID)
    {
        logger(AREA, DEBUG, "Copying Mirage Data Structure To Internal Siemens PLC Parameters For Robot " + robotID);

        try
        { 
            logger(AREA, DEBUG, "Mode is: " + mirFleet.robots[robotID].s.mode_id);

            logger(AREA, DEBUG, "Mission Status is: " + mirFleet.robots[robotID].schedule.state_id);
            SiemensPLC.robots[robotID].Param[5].print();
            SiemensPLC.robots[robotID].Param[5].setValue(mirFleet.robots[robotID].schedule.state_id);
            SiemensPLC.robots[robotID].Param[5].print();

            logger(AREA, DEBUG, "Robot Group is: " + mirFleet.robots[robotID].s.robot_group_id);

            SiemensPLC.robots[robotID].Param[6].print();
            SiemensPLC.robots[robotID].Param[6].setValue(mirFleet.robots[robotID].s.robot_group_id);
            SiemensPLC.robots[robotID].Param[6].print();

            logger(AREA, DEBUG, "Robot Status is: " + mirFleet.robots[robotID].s.state_id);
            SiemensPLC.robots[robotID].Param[7].print();
            SiemensPLC.robots[robotID].Param[7].setValue(mirFleet.robots[robotID].s.state_id);
            SiemensPLC.robots[robotID].Param[7].print();

            logger(AREA, DEBUG, "Robot Position X is: " + mirFleet.robots[robotID].s.position.x);
            SiemensPLC.robots[robotID].Param[8].print();
            SiemensPLC.robots[robotID].Param[8].setValueDouble(mirFleet.robots[robotID].s.position.x);
            SiemensPLC.robots[robotID].Param[8].print();

            SiemensPLC.robots[robotID].Param[9].print();
            SiemensPLC.robots[robotID].Param[9].setValueDouble(mirFleet.robots[robotID].s.position.y);
            SiemensPLC.robots[robotID].Param[9].print();

            SiemensPLC.robots[robotID].Param[10].print();
            SiemensPLC.robots[robotID].Param[10].setValueDouble(mirFleet.robots[robotID].s.position.orientation);
            SiemensPLC.robots[robotID].Param[10].print();

            SiemensPLC.robots[robotID].Param[11].print();
            SiemensPLC.robots[robotID].Param[11].setValueDouble(mirFleet.robots[robotID].s.moved);
            SiemensPLC.robots[robotID].Param[11].print();

            SiemensPLC.robots[robotID].Param[12].print();
            SiemensPLC.robots[robotID].Param[12].setValueDouble(mirFleet.robots[robotID].s.battery_percentage);
            SiemensPLC.robots[robotID].Param[12].print();
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "Failed To Copy Internal Memory To PLC Buffer: ");
            logger(AREA, ERROR, "Exception: ", e);
        }

        logger(AREA, DEBUG, "Completed Copying Mirage Internal Memory For Robot " + robotID + " to PLC Buffer");
    }

    /// <summary>
    /// Saves the internal Mirage memory to the PLC buffer used for writes and reads
    /// </summary>
    public static void allMemoryToPLC()
    {
        fleetMemoryToPLC();

        for (int i = 0; i < sizeOfFleet; i++)
        {
            robotMemoryToPLC(i);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="debug"></param>
    /// <param name="message"></param>
    public static void logger(Type type, DebugLevel debug, string message)
    {
        log = LogManager.GetLogger(type);

        switch (debug)
        {
            case DebugLevel.INFO:
                log.Info(message);
                break;
            case DebugLevel.DEBUG:
                if(debugLevel > 1)
                { 
                    log.Debug(message);
                }
                break;
            case DebugLevel.WARNING:
                log.Warn(message);
                break;
            case DebugLevel.ERROR:
                log.Error(message);
                // Send an SMS message
                break;
            case DebugLevel.FATAL:
                log.Error(message);
                // Send an SMS message
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="debug"></param>
    /// <param name="message"></param>
    /// <param name="exception"></param>
    public static void logger(Type type, DebugLevel debug, string message, Exception exception)
    {
        log = LogManager.GetLogger(type);

        switch (debug)
        {
            case DebugLevel.INFO:
                log.Info(message, exception);
                break;
            case DebugLevel.DEBUG:
                if (debugLevel > 1)
                {
                    log.Debug(message, exception);
                }
                break;
            case DebugLevel.WARNING:
                log.Warn(message, exception);
                break;
            case DebugLevel.ERROR:
                log.Error(message, exception);
                // Send an SMS message
                break;
        }
    }
}
