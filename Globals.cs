using Mirage.plc;
using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

/*  D E B U G    L E V E L S
 *  0 - No Debug, just standard messages 
 *  1 - Events
 *  2 - ...
 *  4 - Everything
*/

/* Contains global variables used by all of the classes.
 * 
 * 
 * 
*/
public static class Globals
{
    public static bool keepRunning = true;
    public static bool resumingSession = false;
    public static int debugLevel = 0, pollInterval, sizeOfFleet;
    public static string logFile, emailAlert, baseURL;
    public static MySqlConnection db;
    public static HttpClient comms;

    // Used for issuing tasks to the fleet or robots
    // Based on PLC input
    //public enum Tasks:int
    public static class Tasks
    {
        public const int SchedulerStatus = 100;
        public const int SchedulerSendMission = 101;
        public const int SchedulerCreateMission = 102;
        public const int SchedulerClear = 103;
        public const int Battery = 200;
        public const int Distance = 201;
        public const int RobotStatus = 202;
    }

    // Status codes for PLC
    public static class Status
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

    public static void readAllSettings()
    {
        Console.Clear();

        Console.WriteLine("MiRage Data Harvester v0.01");
        Console.WriteLine("Property of Iconsys");

        try
        {
            var appSettings = ConfigurationManager.AppSettings;

            if (appSettings.Count == 0)
            {
                Console.WriteLine("AppSettings Branch Within App.config Is Empty.");
                // Send an email alert???
            }
            else
            {
                // Need to cast vars as default type is string
                debugLevel = int.Parse(ConfigurationManager.AppSettings["debugLevel"]);
                pollInterval = int.Parse(ConfigurationManager.AppSettings["pollInterval"]) * 1000; // Convert to seconds
                sizeOfFleet = int.Parse(ConfigurationManager.AppSettings["sizeOfFleet"]);
                logFile = ConfigurationManager.AppSettings["logFile"];
                emailAlert = ConfigurationManager.AppSettings["emailAlert"];
                resumingSession = bool.Parse(ConfigurationManager.AppSettings["resumingSession"]);

                Console.WriteLine("Do you want to start a new session? (y/n)");
                string newSession = Console.ReadLine();

                if (newSession == "y")
                    resumingSession = false;
                else if (newSession == "n")
                    resumingSession = true;
                else
                    Console.WriteLine("The answer must be either 'y' or 'n'");
                // goto -> above

                Console.WriteLine("The fleet has {0} robots", sizeOfFleet);
                Console.WriteLine("Polling occurs every {0} seconds", int.Parse(ConfigurationManager.AppSettings["pollInterval"]));
                Console.WriteLine("Debug Level is set to {0}", debugLevel);

                if (debugLevel > 0)
                {
                    foreach (var key in appSettings.AllKeys)
                    {
                        Console.WriteLine("{0} is set to {1}", key, appSettings[key]);
                    }
                }
            }
        }
        catch (ConfigurationErrorsException)
        {
            Console.WriteLine("==== Error reading app settings ====");
            // TODO: Use default values or send an email and terminate?
        }

        const string accountSid = "ACc9a9248dd2a1f6d6e673148e73cfc2f9";
        const string authToken = "b57abe0211b4fde95bf7ae159eb75e2d";

        string phone_rx = "+447583098757";
        string phone_twilio = "+12512500577";
        string msg_body = "Test Alert from Mirage";

        // Initialize SMS communication for alerts
        TwilioClient.Init(accountSid, authToken);

        // Send Test SMS
        //MessageResource.Create(
        //    to: new PhoneNumber(phone_rx),
        //    from: new PhoneNumber(phone_twilio),
        //    body: msg_body);
    }

    public static void connectToDB()
    {
        if (debugLevel > -1)
            Console.WriteLine("==== Connecting To Databases ====");
        try
        {
            db = new MySqlConnection(ConfigurationManager.ConnectionStrings["master"].ConnectionString);
            db.Open();

            if (debugLevel > -1)
                Console.WriteLine("Local Master DB Connection Established");
        }
        catch (MySqlException ex)
        {
            Console.WriteLine("Local Master DB Connection Failed");
            Console.WriteLine(ex);
            // Print MySQL exception
            // Send Email

            Console.WriteLine("Attempting A Connation With Local Slave DB");

            try
            {
                db = new MySqlConnection(ConfigurationManager.ConnectionStrings["slave"].ConnectionString);
                db.Open();

                if (debugLevel > -1)
                {
                    Console.WriteLine("Local Slave DB Connection Established");
                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Local Slave DB Connection Failed");
                Console.WriteLine(e);
                // Print MySQL exception
                // Send email and terminate process?
                keepRunning = false;
            }
        }
    }

    public static void setUpDefaultComms()
    {
        // TODO: Set up httpClient as a service to make network debugging easier

        if (debugLevel > -1)
            Console.WriteLine("==== Setting Up Default API Connection Details ====");

        // TODO: Catch Exceptions if they exist (maybe a null exception?)
        comms = new HttpClient();
        comms.DefaultRequestVersion = HttpVersion.Version11;
        comms.DefaultRequestHeaders.Accept.Clear();
        comms.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        comms.DefaultRequestHeaders.Add("Accept-Language", "en_US");
        comms.Timeout = TimeSpan.FromMinutes(10);

        // Establish PLC Connection TODO: add error handling
        SiemensPLC.establishConnection();
    }

    public static void closeComms()
    {
        if (debugLevel > -1)
            Console.WriteLine("==== Closing Socket Connections ====");

        comms.Dispose();
        SiemensPLC.disconnect();
    }

    public static void logJSON(string json)
    {
        Console.WriteLine(json);
        issueInsertQuery("INSERT INTO mir.logger(DATA) values ('" + MySqlHelper.EscapeString(json) + "');");
    }

    public static void issueInsertQuery(string query)
    {
        int rowsAffected = 0;

        if (debugLevel > 3)
            Console.WriteLine(query);

        try
        {
            MySqlCommand insertQuery = new MySqlCommand(query, db);
            rowsAffected = insertQuery.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                // Query Failed
                Console.WriteLine("Insert Query Hasn't Been Stored");
            }
            else if (debugLevel > 0)
            {
                Console.WriteLine("Insert Query Stored Successfully");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed To Insert");
            Console.WriteLine(e);
        }
    }

    public static int? getIDQuery(string table)
    {
        int? id = null;

        if (debugLevel > 1)
            Console.WriteLine(table);

        try
        {
            MySqlCommand getQuery = new MySqlCommand("SELECT MAX(" + table + "_id) FROM " + table, db);


            if (debugLevel > 1)
                Console.WriteLine(getQuery);

            object result = getQuery.ExecuteScalar();
            if (result != null)
                id = (int)Convert.ToUInt64(result);
            else
                Console.WriteLine("We've got a null");
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed To Insert");
            Console.WriteLine(e);
        }

        return id;
    }

    public static object addToDB(object value)
    {
        if (null != value)
        {
            if(debugLevel > 1)
            { 
                Console.WriteLine("Value isn't null");
                Console.WriteLine("Value is: " + value.ToString());
            }

            if (value.ToString() == "''")
            {
                return "NULL,";
            }
            else if (value.ToString() == "false")
            {
                return "'0',";
            }
            else if (value.ToString() == "true")
            {
                return "'1',";
            }
            else
                return "'" + value + "',";
        }
        else
        {
            if(debugLevel > 1)
                Console.WriteLine("Value is null");

            return "NULL,";
        }
    }

    public static void checkAlertsAndErrors()
    {
        int alert = 0;
        int fatal = 10;

        if(alert < fatal)
        {
            // If a non-fatal alert, store in the log file (or DB) 
            // and send an email (through Grafana?)
        }
        else if(alert > fatal)
        {
            // If we've got a fatal alert, we want to terminate the program
            // still do it gracefully. Send an SMS alert, email, store in log




            keepRunning = false;
        }
    }

    public static void AddUpdateAppSettings(string key, string value)
    {
        try
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;

            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }

            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
        catch (ConfigurationErrorsException)
        {
            Console.WriteLine("Error writing app settings");
        }
    }
}
