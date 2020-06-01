using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

/* Contains global variables used by all of the classes.
 * 
 * 
 * 
*/
public static class Globals
{
    public static bool keepRunning = true;
    public static int debugLevel, pollInterval, numberOfRobots;
    public static string logFile, emailAlert, baseURL;
    public static MySqlConnection db;
    public static HttpClient comms;

    public static void readAllSettings()
    {
        if (debugLevel > 0)
            Console.WriteLine("==== Fetching App Settings From App.config ====");

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
                pollInterval = int.Parse(ConfigurationManager.AppSettings["pollInterval"]);
                numberOfRobots = int.Parse(ConfigurationManager.AppSettings["numberOfRobots"]);
                logFile = ConfigurationManager.AppSettings["logFile"];
                emailAlert = ConfigurationManager.AppSettings["emailAlert"];
                baseURL = ConfigurationManager.AppSettings["baseURL"];

                if (debugLevel > 0)
                {
                    foreach (var key in appSettings.AllKeys)
                    {
                        Console.WriteLine("Key: {0} Value: {1}", key, appSettings[key]);
                    }
                }
            }
        }
        catch (ConfigurationErrorsException)
        {
            Console.WriteLine("==== Error reading app settings ====");
            // TODO: Use default values or send an email and terminate?
        }
    }

    public static void connectToDB()
    {
        if (debugLevel > 0)
            Console.WriteLine("==== Connecting To Databases ====");
        try
        {
            db = new MySqlConnection(ConfigurationManager.ConnectionStrings["master"].ConnectionString);
            db.Open();

            if (debugLevel > 0)
                Console.WriteLine("Local Master DB Connection Established");
        }
        catch (MySqlException ex)
        {
            Console.WriteLine("Local Master DB Connection Failed");
            // Print MySQL exception
            // Send Email

            Console.WriteLine("Attempting A Connation With Local Slave DB");

            try
            {
                db = new MySqlConnection(ConfigurationManager.ConnectionStrings["slave"].ConnectionString);
                db.Open();

                if (debugLevel > 0)
                {
                    Console.WriteLine("Local Slave DB Connection Established");
                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine("Local Slave DB Connection Failed");
                // Print MySQL exception
                // Send email and terminate process?
                keepRunning = false;
            }
        }
    }

    public static void setUpDefaultComms()
    {
        // TODO: Set up httpClient as a service to make network debugging easier

        if (debugLevel > 0)
            Console.WriteLine("==== Setting Up Default API Connection Details ====");

        // TODO: Catch Exceptions if they exist (maybe a null exception?)
        comms = new HttpClient();
        comms.DefaultRequestVersion = HttpVersion.Version11;
        comms.DefaultRequestHeaders.Accept.Clear();
        comms.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        comms.DefaultRequestHeaders.Add("Accept-Language", "en_US");
        comms.Timeout = TimeSpan.FromMinutes(10);
    }

    public static void closeComms()
    {
        if (debugLevel > 0)
            Console.WriteLine("==== Closing Socket Connections ====");

        comms.Dispose();
    }

    public static void logJSON(string json)
    {
        issueInsertQuery("INSERT INTO mir.logger(DATA) values ('" + json + "');");
    }

    public static void issueInsertQuery(string query)
    {
        int rowsAffected = 0;

        if (debugLevel > 0)
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
            if(debugLevel > 0)
            { 
                Console.WriteLine("Value isn't null");
                Console.WriteLine("Value is: " + value.ToString());
            }

            if (value.ToString() == "''")
            { 
                return "NULL,";
            }
            else if(value.ToString() == "false")
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
            if(debugLevel > 0)
                Console.WriteLine("Value is null");

            return "NULL,";
        }
    }
}
