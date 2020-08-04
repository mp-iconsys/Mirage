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
using static Globals.DebugLevel;
using System.Collections.Generic;

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

    public static string fleetManagerIP;
    public static AuthenticationHeaderValue fleetManagerAuthToken;
    public static Fleet mirFleet;

    //=========================================================|
    //  Used For Logging & Debugging                           |     
    //=========================================================|
    public static int debugLevel = 0;
    private static readonly Type AREA = typeof(Globals);
    public static ILog log = LogManager.GetLogger(AREA);

    //=========================================================|
    //  For Sending SMS Alerts                                 |     
    //=========================================================|
    private static string accountSid; //= "ACc9a9248dd2a1f6d6e673148e73cfc2f9";
    private static string authToken; //= "b57abe0211b4fde95bf7ae159eb75e2d";
    private static string phone_rx;
    private static List<string> phone_numbers;
    private static string phone_twilio;

    /// <summary>
    /// Different Levels For Printing Debug Messages
    /// </summary>
    public enum DebugLevel
    {
        INFO = 1,
        DEBUG = 2,
        WARNING = 3,
        ERROR = 4
    }

    /// <summary>
    /// Used for issuing tasks to the fleet or robots. Matches PLC Task Codes. Does not conatin any methods.
    /// </summary>
    public static class Tasks
    {
        public const int GetScheduleStatus = 100;
        public const int SendMissionToScheduler = 101;
        public const int CreateMission = 102;
        public const int ClearScheduler = 103;
        public const int GetBattery = 200;
        public const int GetDistance = 201;
        public const int GetRobotStatus = 202;
    }

    /// <summary>
    /// Task Status Codes that match PLC Task Status. Does not conatin any methods.
    /// </summary>
    public static class TaskStatus
    {
        public const int Awaiting = 0;
        public const int StartedProcessing = 1;
        public const int CompletedNoErrors = 20;
        public const int CompletedPartially = 21;
        public const int PlcOK = 30;
        public const int PlcError = 31;
        public const int CouldntProcessRequest = 40;
        public const int FatalError = 41;
    }

    /// <summary>
    /// Reads settings from various config files. They are: log4net.config, plc_config.xml and App.config.
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

        logger(AREA, INFO, "Starting Mirage Data Harvester v0.01");
        logger(AREA, DEBUG, "==== Obtaining Settings ====");

        try
        {
            connectToDB();

            fleetManagerAuthToken = new AuthenticationHeaderValue("Basic", "moo");
            fleetManagerIP = "192.168.0.1";

            var appSettings = ConfigurationManager.AppSettings;

            if (appSettings.Count == 0)
            {
                logger(AREA, ERROR, "AppSettings Branch Within App.config Is Empty");
                logger(AREA, ERROR, "Terminating the application.");

                gracefulTermination("Failed to read application settings, on start-up. Check app.config.exe file. App will terminate");
            }
            else
            {
                debugLevel      = int.Parse(ConfigurationManager.AppSettings["debugLevel"]);
                pollInterval    = int.Parse(ConfigurationManager.AppSettings["pollInterval"]); 
                sizeOfFleet     = int.Parse(ConfigurationManager.AppSettings["sizeOfFleet"]);
                resumingSession = bool.Parse(ConfigurationManager.AppSettings["resumingSession"]);
                phone_rx        = ConfigurationManager.AppSettings["phone_rx"];
                phone_twilio    = ConfigurationManager.AppSettings["phone_twilio"];

                Console.WriteLine("Do you want to start a new session? (y/n)");
                string newSession = Console.ReadLine();

                if (newSession == "y")
                { 
                    resumingSession = false;
                }
                else if (newSession == "n")
                { 
                    resumingSession = true;
                }
                else
                { 
                    Console.WriteLine("The answer must be either 'y' or 'n'");
                    // goto -> above
                }

                logger(AREA, INFO, "The Fleet Has " + sizeOfFleet + " Robotsr");
                logger(AREA, INFO, "Data Is Sarvested Every " + pollInterval + " Seconds");
                logger(AREA, INFO, "Debug Level Is" + debugLevel);

                foreach (var key in appSettings.AllKeys)
                {
                    logger(AREA, DEBUG, key + " is set to " + appSettings[key]);
                }

                logger(AREA, INFO, "Settings Fetched Successfully");
            }
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
        //  Initialize Fleet Container                             |     
        //=========================================================|
        mirFleet = new Fleet();

        logger(AREA, DEBUG, "==== Finished Obtaining Settings ====");
    }

    /// <summary>
    /// Establishes a connection to master database. If not available, it switches over to a local slave DB.
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
            logger(AREA, INFO, "Attempting to connect to local server");

            try
            {
                db = new MySqlConnection(ConfigurationManager.ConnectionStrings["slave"].ConnectionString);
                db.Open();

                logger(AREA, INFO, "Connected To Slave DB");
                sendSMS("Failed to connect to both master database.");
            }
            catch (MySqlException e)
            {
                logger(AREA, ERROR, "Local connection failed with error: ", e);
                sendSMS("Failed to connect to both master and slave databases. Check if they're up and if the network is blocked.");
            }
        }
    }

    /// <summary>
    /// Obtains application settings from the Database
    /// </summary>
    public static void readSettingsFromDB()
    {
        //=========================================================|
        //  Read App Configuration                                 |     
        //=========================================================|
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
        }

        //=========================================================|
        //  Read SMS Phone Numbers For Alerts                      |     
        //=========================================================|
        sql = "SELECT * FROM alert_phone_numbers;";
        using var cmd1 = new MySqlCommand(sql, db);
        using MySqlDataReader rdr1 = cmd1.ExecuteReader();
        int i = 0;

        while (rdr.Read())
        {
            logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - Number: " + rdr.GetString(1));
            phone_numbers[i] = rdr.GetString(1);
            i++;
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

            logger(AREA, INFO, "MiR HTTP Connections Established");
        }
        catch(Exception exception)
        {
            logger(AREA, ERROR, "Failed to set up an HTTP Connection. Error: ", exception);
        }

        //SiemensPLC.establishConnection();

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
    /// <param name="json"></param>
    public static void logJSON(string json)
    {
        Console.WriteLine(json);
        //issueInsertQuery("INSERT INTO mir.logger(DATA) values ('" + MySqlHelper.EscapeString(json) + "');");
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

            //Console.WriteLine("Rows Affected: " + rowsAffected);

            if (rowsAffected == 0)
            {
                // Query Worked (for transactions and procedures)
                //Console.WriteLine("Insert Query Hasn't Been Stored");
            }
            else if (debugLevel > 0)
            {
                Console.WriteLine("Insert Query Stored Successfully");
            }

            cmd.Dispose();
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed To Insert");
            Console.WriteLine(e);
        }

        logger(AREA, DEBUG, "==== Closing Connections ====");
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
        logger(AREA, DEBUG, "==== Sending SMS Alert ====");

        try
        { 
            MessageResource.Create(
                to: new PhoneNumber(phone_rx),
                from: new PhoneNumber(phone_twilio),
                body: message);

            logger(AREA, INFO, "Sending An SMS Alert: " + message);
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "Failed To Send SMS. Error: ", exception);
        }

        logger(AREA, DEBUG, "==== SMS Alert Sent ====");
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
