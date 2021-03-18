﻿using System;
using System.Xml;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;
using static DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave.libnodave;
using System.Collections.Generic;
using Mirage.plc;
using System.Collections;

/// <summary>
/// Contains all the methods and data related to a Siemens PLC.
/// </summary>
/// <remarks>
/// The data is divided into three parts: connection details,
/// storage of the task control and storage of data itself.
/// The methods are primarily for establishing a connection,
/// writing and reading from a PLC.
/// </remarks>
class SiemensPLC
{
    //=========================================================|
    //  Libnodave driver types, used to form comms with PLC    |         
    //=========================================================|
    private static daveOSserialType fds;
    private static daveInterface di;
    private static daveConnection dc;

    //=========================================================|
    //  For S7-1200 and 1500, use rack = 0, slot = 1           |
    //  IP and port are fetched at initialization, from config |
    //=========================================================|
    private static string IP;
    private static int port;
    private static int rack = 0;
    private static int slot = 1;

    //=========================================================|
    //  These should reflect the data blocks in the PLC        |
    //                                                         |
    //  We're assuming that each block is completely dedicated |
    //  to storing only this data, so they start at offset 0.  |  
    //  TODO: enable offsets for blocks?                       |
    //=========================================================|
    private static int taskControlDB;
    private static int dataStorageDB;

    //=========================================================|
    //  PLC Task Control Block                                 |
    //=========================================================|
    public static int serialNumber = -1; // So we don't risk accidently having the same serial number as in PLC on restart
    public static int robotID;
    public static int task;
    public static int status;
    public static int parameter;

    //=========================================================|
    //  Contains the structure for the robots.                 |
    //  Both Task Control and Data are in the same DB.         |
    //=========================================================|
    public static RobotBlock fleetBlock;
    public static int fleetID = 666;
    public static int fleetBlockControlParameters = 4;
    public static int fleetBlockSize;
    public static bool furtherMsg;

    public static RobotBlock[] robots;
    public static int noOfRobots;
    public static int robotBlockSize;
    public static int robotBlockControlParameters = 4;

    public static int alarmOffset = 424;
    public static int alarmBlockSize = 22;
   // public static BitArray[] alrm;
    public static Alarms plcAlarms = new Alarms();

    //=========================================================|
    //  Message helper parameters                              |
    //=========================================================|
    public static bool[] newMsgs = new bool[10];
    public static bool newMsg;
    public static bool plcConnected;
    public static bool live;

    //=========================================================|
    //  Used For Logging & Debugging                           |     
    //=========================================================|
    public static int plcConnectionErrors = 0;
    private static readonly Type AREA = typeof(SiemensPLC);

    /// <summary>
    /// Initializes the PLC, either through the database or through the PLC config file.
    /// </summary>
    public static void initialize()
    {
        logger(AREA, DEBUG, "==== Starting Initialization ====");

        for(int i = 0; i < sizeOfFleet+1; i++)
        {
            newMsgs[i] = false;
        }

        int rowcount = 0;

        // First, try to get configuration data from the database
        try
        {
            string sql = "SELECT * FROM plc_config;";
            using var cmd = new MySqlCommand(sql, db);
            using MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                rowcount++;

                logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - Variable: " + rdr.GetString(1) + " - Value: " + rdr.GetString(2));

                if (rdr.GetString(1) == "IP")
                {
                    IP = rdr.GetString(2);
                }
                else if (rdr.GetString(1) == "Port")
                {
                    port = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "rack")
                {
                    rack = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "slot")
                {
                    slot = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "taskControlDB")
                {
                    taskControlDB = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "dataStorageDB")
                {
                    dataStorageDB = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "Live")
                {
                    live = Boolean.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "NoOfRobots")
                {
                    noOfRobots = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "RobotBlockSize")
                {
                    robotBlockSize = Int32.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "FleetBlockSIze")
                {
                    fleetBlockSize = Int32.Parse(rdr.GetString(2));
                }
            }
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "==== Failed To Fetch Config Data From Database ====");
            logger(AREA, ERROR, "Exception: ", e);
        }

        // If we don't have all the configs from the database, read from the file
        if (rowcount < 9)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(@"config\plc.config");

                IP = doc.DocumentElement.SelectSingleNode("/configuration/plc/connectionString/ip").InnerText;
                port = short.Parse(doc.DocumentElement.SelectSingleNode("/configuration/plc/connectionString/port").InnerText);
                rack = short.Parse(doc.DocumentElement.SelectSingleNode("/configuration/plc/connectionString/rack").InnerText);
                slot = short.Parse(doc.DocumentElement.SelectSingleNode("/configuration/plc/connectionString/slot").InnerText);
                taskControlDB = short.Parse(doc.DocumentElement.SelectSingleNode("/configuration/plc/data/taskControlDB").InnerText);
                dataStorageDB = short.Parse(doc.DocumentElement.SelectSingleNode("/configuration/plc/data/dataStorageDB").InnerText);
                live = false;

                logger(AREA, DEBUG, "IP : " + IP);
                logger(AREA, DEBUG, "Port : " + port);
                logger(AREA, DEBUG, "Rack : " + rack);
                logger(AREA, DEBUG, "Slot : " + slot);
                logger(AREA, DEBUG, "Task Control DB No : " + taskControlDB);
                logger(AREA, DEBUG, "Data Storage DB No : " + dataStorageDB);
                logger(AREA, DEBUG, "Simulating : " + live);
            }
            catch (Exception exception)
            {
                keepRunning = false;
                logger(AREA, ERROR, "Failed to load PLC configuration file. Mirage will terminate. Exception : ", exception);
            }
        }

        logger(AREA, DEBUG, "==== Getting Robot Area Block Structure ====");

        rowcount = 0;

        // Alrigh, we're setting up corresponding data blocks for talking with PLC
        try
        {
            //=========================================================|
            //  Instantiate fleet block                                |
            //=========================================================|
            fleetBlock = new RobotBlock(fleetID, 0, fleetBlockSize);

            //=========================================================|
            //  Get Parameters from DB                                 |
            //=========================================================|
            try
            {
                string sql = "SELECT * FROM plc_fleet_block;";
                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                // This is reading the parameters
                while (rdr.Read())
                {
                    logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - Parameter: " + rdr.GetString(1) + " - Size: " + rdr.GetString(2) + " - Datatype: " + rdr.GetString(3));

                        string param_name = rdr.GetString(1);
                        int param_size = Int32.Parse(rdr.GetString(2));
                        int param_offset = fleetBlock.Offset;

                        if (rdr.GetString(3) == "INT")
                        {
                            if (rowcount != 0)
                            {
                                // The offset for the current parameter is equal to:
                                //  Robot Offset + Previous Parameter Offset + Previous Parameter Size
                                param_offset = fleetBlock.Param[rowcount - 1].getOffset() + fleetBlock.Param[rowcount - 1].getSize();
                            }

                            Parameter_INT temp = new Parameter_INT(param_name, param_size, param_offset);

                            fleetBlock.Param.Add(temp);
                        }
                        else if (rdr.GetString(3) == "FLOAT")
                        {
                            if (rowcount != 0)
                            {
                                // The offset for the current parameter is equal to:
                                //  Robot Offset + Previous Parameter Offset + Previous Parameter Size
                                param_offset = fleetBlock.Param[rowcount - 1].getOffset() + fleetBlock.Param[rowcount - 1].getSize();
                            }

                            Parameter_FLOAT temp = new Parameter_FLOAT(param_name, param_size, param_offset);

                            fleetBlock.Param.Add(temp);
                        }

                    rowcount++;
                }
            }
            catch(Exception)
            {

            }

            //=========================================================|
            //  Instantiate robot array                                |
            //=========================================================|
            robots = new RobotBlock[noOfRobots];

            for (int i = 0; i < noOfRobots; i++)
            {   
                int id = i;
                int offset = (fleetBlockSize + robotBlockSize) + (i * robotBlockSize);

                robots[i] = new RobotBlock(id, offset);
            }

            //=========================================================|
            //  Get Robot Parameters from DB                           |
            //=========================================================|
            try
            {
                rowcount = 0;

                string sql = "SELECT * FROM plc_robot_block;";
                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                // This is reading the parameters
                while (rdr.Read())
                {
                    logger(AREA, DEBUG, "Row: " + rdr.GetInt32(0) + " - Parameter: " + rdr.GetString(1) + " - Size: " + rdr.GetString(2) + " - Datatype: " + rdr.GetString(3));

                    for (int i = 0; i < noOfRobots; i++)
                    {
                        string param_name = rdr.GetString(1);
                        int param_size = Int32.Parse(rdr.GetString(2));
                        int param_offset = robots[i].Offset;

                        if (rdr.GetString(3) == "INT")
                        {
                            if (rowcount != 0)
                            {
                                // The offset for the current parameter is equal to:
                                //  Robot Offset + Previous Parameter Offset + Previous Parameter Size
                                param_offset = robots[i].Param[rowcount - 1].getOffset() + robots[i].Param[rowcount - 1].getSize();
                            }

                            Parameter_INT temp = new Parameter_INT(param_name, param_size, param_offset);

                            robots[i].Param.Add(temp);
                        }
                        else if(rdr.GetString(3) == "FLOAT")
                        {
                            if (rowcount != 0)
                            {
                                // The offset for the current parameter is equal to:
                                //  Robot Offset + Previous Parameter Offset + Previous Parameter Size
                                param_offset = robots[i].Param[rowcount - 1].getOffset() + robots[i].Param[rowcount - 1].getSize();
                            }

                            Parameter_FLOAT temp = new Parameter_FLOAT(param_name, param_size, param_offset);

                            robots[i].Param.Add(temp);
                        }
                    }

                    rowcount++;
                }
            }
            catch(Exception)
            {

            }
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "==== Failed To Fetch Robot Block Data From Database ====");
            logger(AREA, ERROR, "Exception: ", e);
        }

        //=========================================================|
        //  Check all the parameters are OK                        |
        //=========================================================|
        for (int j = 0; j < fleetBlock.Param.Count; j++)
        {
            fleetBlock.Param[j].print();
        }

        for (int i = 0; i < noOfRobots; i++)
        {
            for(int j = 0; j < robots[0].Param.Count; j++ )
            {
                robots[i].Param[j].print();
            }
        }

        logger(AREA, INFO, "==== PLC Initialization Completed ====");
    }

    /// <summary>
    /// Connects to a Siemens PLC. Based on information obtained through the initialize function.
    /// </summary>
    public static void establishConnection()
    {
        logger(AREA, DEBUG, "==== Establishing A Connection ====");

        if(live)
        {
            fds.rfd = openSocket(port, IP);
            fds.wfd = fds.rfd;

            if (fds.rfd != IntPtr.Zero)
            {
                logger(AREA, DEBUG, "Socket Opened Successfully");

                di = new daveInterface(fds, "IF1", 0, daveProtoISOTCP, daveSpeed187k);
                di.setTimeout(1000000);
                dc = new daveConnection(di, 0, rack, slot);

                if (0 == dc.connectPLC())
                {
                    logger(AREA, INFO, "Connected To The PLC");
                    plcConnected = true;
                    plcConnectionErrors = 0; // Reset counter on successfull connection
                }
                else
                {
                    logger(AREA, ERROR, "Failed To Connect. Trying again, with result " + dc.connectPLC());
                    logger(AREA, ERROR, daveStrerror(dc.connectPLC()));

                    plcConnectionErrors++;
                    plcConnected = false;
                    newMsg = false;
                }
            }
            else
            {
                logger(AREA, ERROR, "Socket Failed To Open. DaveOSserialType is initialized to " + fds.rfd);
                logger(AREA, ERROR, daveStrerror(fds.rfd.ToInt32()));

                plcConnectionErrors++;
                newMsg = false;
                plcConnected = false;
            }
        }
        else
        {

            logger(AREA, INFO, "==== Running In Sim Mode ====");

            plcConnected = true;
            newMsg = false;
        }

        logger(AREA, DEBUG, "==== Established A Connection ====");
    }

    /// <summary>
    /// Polls the PLC for data regarding new tasks or missions.
    /// </summary>
    public static void poll()
    {
        logger(AREA, DEBUG, "==== Starting To Poll ====");

        readFleetHeader();

        readRobots();

        logger(AREA, DEBUG, "==== Completed Polling ====");
    }

    /// <summary>
    /// Writes data to the Siemens PLC Data Storage block and updates the Status Code.
    /// </summary>
    /// <param name="type">The variable name we want to write to the PLC.</param>
    /// <param name="statusCode">Status code based on the HTTP request.</param>
    /// <param name="data">The data obtained from the MiR/Fleet.</param>
    public static void writeData(string type, int statusCode, float data)
    {
        logger(AREA, DEBUG, "==== Starting To Write Data ====");

        int result = 1;

        if(live)
        {
            logger(AREA, DEBUG, "Type : " + type);
            logger(AREA, DEBUG, "Status Code : " + statusCode);
            logger(AREA, DEBUG, "Data : " + data);

            // First, check if the data response was successful
            if (statusCode == TaskStatus.CompletedPartially || statusCode == TaskStatus.CompletedNoErrors)
            {
                logger(AREA, DEBUG, "Request Completed Or Completed Partially. Status Code : " + statusCode);

                byte[] tempBytes = BitConverter.GetBytes(data);
                if (BitConverter.IsLittleEndian) { Array.Reverse(tempBytes); }

                // TODO: This should really use an enum instead of strings
                if (type == "moved")
                {
                    try
                    {
                        result = dc.writeBytes(daveDB, dataStorageDB, 4, 8, tempBytes);
                    }
                    catch
                    {
                        logger(AREA, ERROR, "Failed To Save Data In PLC. Check PLC Connectivity.");
                    }
                }
                else if (type == "battery")
                {
                    try
                    {
                        result = dc.writeBytes(daveDB, dataStorageDB, 0, 4, tempBytes);
                    }
                    catch
                    {
                        logger(AREA, ERROR, "Failed To Save Data In PLC. Check PLC Connectivity.");
                    }
                }

                if (result != 0)
                {
                    logger(AREA, ERROR, "Failed To Save Data In PLC. Check PLC Connectivity.");
                    logger(AREA, ERROR, daveStrerror(result));
                    plcConnectionErrors++;
                }
                else
                {
                    updateTaskStatus(TaskStatus.CompletedNoErrors);
                }
            }
            else if (statusCode == TaskStatus.CouldntProcessRequest)
            {
                logger(AREA, WARNING, "We Couldn't Process The Request. Status Code : " + statusCode);
                updateTaskStatus(TaskStatus.CouldntProcessRequest);
            }
            else if (statusCode == TaskStatus.FatalError)
            {
                logger(AREA, WARNING, "Fatal Error. Status Code : " + statusCode);
                updateTaskStatus(TaskStatus.FatalError);
            }
            else
            {
                logger(AREA, WARNING, "Unknown Status. Status Code : " + statusCode);
                updateTaskStatus(TaskStatus.FatalError);
                // Unknown Status ??? - > Treat like a fatal error
            }
        }
        else
        {
            logger(AREA, INFO, "==== Running In Simulation Mode ====");
            logger(AREA, INFO, "Type : " + type);
            logger(AREA, INFO, "Status Code : " + statusCode);
            logger(AREA, INFO, "Data : " + data);
        }

        logger(AREA, INFO, "==== Completed Data Write ====");
    }

    /// <summary>
    /// Writes data to the Siemens PLC Data Storage block and updates the Status Code.
    /// </summary>
    /// <param name="type">The variable name we want to write to the PLC.</param>
    /// <param name="statusCode">Status code based on the HTTP request.</param>
    /// <param name="data">The data obtained from the MiR/Fleet.</param>
    public static void writeData(string type, int statusCode, string data)
    {
        logger(AREA, DEBUG, "==== Starting To Write Data ====");

        int result = 1;

        if(live)
        {
            logger(AREA, DEBUG, "Type : " + type);
            logger(AREA, DEBUG, "Status Code : " + statusCode);
            logger(AREA, DEBUG, "Data : " + data);

            // First, check if the data response was successful
            if (statusCode == TaskStatus.CompletedPartially || statusCode == TaskStatus.CompletedNoErrors)
            {
                logger(AREA, DEBUG, "Request Completed Or Completed Partially. Status Code : " + statusCode);

                byte[] tempBytes = System.Text.Encoding.ASCII.GetBytes(data);
                if (BitConverter.IsLittleEndian) { Array.Reverse(tempBytes); }

                if (type == "mission_text")
                {
                    try
                    {
                        result = dc.writeBytes(daveDB, dataStorageDB, 8, 256, tempBytes);
                    }
                    catch
                    {
                        logger(AREA, ERROR, "Failed To Save Data In PLC. Check PLC Connectivity.");
                    }
                }
                else if (type == "mission_schedule")
                {
                    try
                    {
                        result = dc.writeBytes(daveDB, dataStorageDB, 264, 256, tempBytes);
                    }
                    catch
                    {
                        logger(AREA, ERROR, "Failed To Save Data In PLC. Check PLC Connectivity.");
                    }
                }

                if (result != 0)
                {
                    logger(AREA, ERROR, "Failed To Save Data In PLC. Check PLC Connectivity.");
                    logger(AREA, ERROR, daveStrerror(result));
                    plcConnectionErrors++;
                }
            }
            else if (statusCode == TaskStatus.CouldntProcessRequest)
            {
                logger(AREA, WARNING, "We Couldn't Process The Request. Status Code : " + statusCode);
                updateTaskStatus(TaskStatus.CouldntProcessRequest);
            }
            else if (statusCode == TaskStatus.FatalError)
            {
                logger(AREA, WARNING, "Fatal Error. Status Code : " + statusCode);
                updateTaskStatus(TaskStatus.FatalError);
            }
            else
            {
                logger(AREA, WARNING, "Unknown Status. Status Code : " + statusCode);
                updateTaskStatus(TaskStatus.FatalError);
                // Unknown Status ??? - > Treat like a fatal error
            }
        }
        else
        {
            logger(AREA, INFO, "==== Running In Simulation Mode ====");
            logger(AREA, INFO, "Type : " + type);
            logger(AREA, INFO, "Status Code : " + statusCode);
            logger(AREA, INFO, "Data : " + data);
        }

        logger(AREA, INFO, "==== Completed Data Write ====");
    }

    public static void readFleetHeader()
    {
        logger(AREA, DEBUG, "==== Reading Fleet Header====");

        if (live)
        {
            int memoryres;
            byte[] memoryBuffer = new byte[fleetBlockControlParameters * 2];

            try
            {
                taskControlDB = 19;

                // readBytes(Area, Data Block Number (in PLC), Start Byte, Length, Byte Container)
                memoryres = dc.readBytes(daveDB, taskControlDB, fleetBlock.Offset, fleetBlockControlParameters * 2, memoryBuffer);

                //=========================================================|
                //  Memoryres - return code from Libnodave:                |
                //    0 - Obtained Data                                    |
                //  < 0 - Error detected by Libnodave                      |
                //  > 0 - Error from the PLC                               |
                //=========================================================|
                if (memoryres == 0)
                {
                    logger(AREA, DEBUG, BitConverter.ToString(memoryBuffer));

                    for (int i = 0; i < fleetBlockControlParameters; i++)
                    {
                        int size = 2;
                        int byte1 = i * size;
                        int byte2 = (i * size) + 1;

                        byte[] tempBytesForConversion = new byte[2] { memoryBuffer[byte1], memoryBuffer[byte2] };

                        // Need to reverse the bytes to get actual values
                        if (BitConverter.IsLittleEndian)
                        { Array.Reverse(tempBytesForConversion); }

                        if(i == 0)
                        {
                            if(BitConverter.ToInt16(tempBytesForConversion, 0) == 10 && fleetBlock.Param[0].getValue() == 0)
                            {
                                logger(AREA, DEBUG, "Got A Mission From Fleet");
                                newMsgs[0] = true;

                                for (int j = 0; j < fleetBlockControlParameters; j++)
                                {
                                    fleetBlock.Param[j].print();
                                }

                                updateTaskStatus(fleetID, 10);
                                newMsg = true;
                            }
                            else
                            {
                                newMsg = false;
                                logger(AREA, DEBUG, "PLC Fleet Block Idle");
                                newMsgs[0] = false;
                            }
                        }

                        fleetBlock.Param[i].setValue(BitConverter.ToInt16(tempBytesForConversion, 0));
                    }

                    // Not sure this is right
                    // Might have to only trigger new message on rising edge 
/*                    if (fleetBlock.Param[0].getValue() == TaskStatus.AwaitingPickUp)
                    {
                        newMsg = true;

                        for (int i = 0; i < fleetBlockControlParameters; i++)
                        {
                            fleetBlock.Param[i].print();
                        }

                        // TaskStatus.TaskReceivedFromPLC
                        updateTaskStatus(fleetID, 10);
                    }
                    else if (fleetBlock.Param[0].getValue() == 0)
                    {
                        logger(AREA, DEBUG, "PLC Fleet Block Idle");
                        newMsg = false;
                    }*/
                }
                else
                {
                    logger(AREA, ERROR, "Failed to Poll");
                    logger(AREA, ERROR, daveStrerror(memoryres));
                    plcConnectionErrors++;
                    newMsg = false;
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                plcConnectionErrors++;
                newMsg = false;

                establishConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Polling Failed. Error : ", exception);
                plcConnectionErrors++;
                newMsg = false;
            }
        }
        else
        {
            logger(AREA, INFO, "Simulating Messages From Console");

            if (!newMsg)
            {
                for (int i = 0; i < fleetBlockControlParameters + 1; i++)
                {
                    fleetBlock.Param[i].simulateConsole();
                }

                string yn;

                Console.WriteLine("Add Robot Commands Further On (y/n)");
                yn = Console.ReadLine();

                if (yn == "y" || yn == "Y")
                {
                    furtherMsg = true;
                }
                else if (yn == "n" || yn == "N")
                {
                    furtherMsg = false;
                }

                newMsg = true;
            }
        }

        logger(AREA, DEBUG, "==== Printing Fleet Task Control ====");

        for (int i = 0; i < fleetBlockControlParameters; i++)
        {
            fleetBlock.Param[i].print();
        }

        //Console.ReadLine();

        logger(AREA, DEBUG, "==== Completed Fleet Header ====");
    }

    public static void readRobots()
    {
        logger(AREA, DEBUG, "==== Reading Robot Task Control ====");

        if (live)
        {
            // TODO: Check the size of robotBlockSize buffer. 
            // If bigger than 222, break down into robots?

            int memoryres;
            byte[] memoryBuffer = new byte[robotBlockControlParameters * noOfRobots * 2];

            logger(AREA, DEBUG, "Initial Memory Buffer: ");
            logger(AREA, DEBUG, BitConverter.ToString(memoryBuffer));

            try
            {
                taskControlDB = 19;

                logger(AREA, DEBUG, "DB: " + taskControlDB + "   Initial Offset: " + robots[0].Offset + "    Array Size: " + robotBlockControlParameters * noOfRobots);

                // readBytes(Area, Data Block Number (in PLC), Start Byte, Length, Byte Container)
                memoryres = dc.readBytes(daveDB, taskControlDB, robots[0].Offset, robotBlockControlParameters * noOfRobots * 2, memoryBuffer);

                //=========================================================|
                //  Memoryres - return code from Libnodave:                |
                //    0 - Obtained Data                                    |
                //  < 0 - Error detected by Libnodave                      |
                //  > 0 - Error from the PLC                               |
                //=========================================================|
                if (memoryres == 0)
                {

                    logger(AREA, DEBUG, "Memory Buffer After Libnodave Read: ");
                    logger(AREA, DEBUG, BitConverter.ToString(memoryBuffer));

                    //================================================================| 
                    // For each robot, go through the first (control) parameters.     |
                    // Read them and save the data in the robotBlock structure.       |
                    // If any TaskStatus is 10, that means we've got tasks to do.     |
                    //================================================================|
                    for (int r = 0; r < noOfRobots; r++)
                    {
                        logger(AREA, DEBUG, "Going Through Robot : " + r);

                        //int byteOffset = robots[r].Offset;
                        int byteOffset = 0;

                        for (int i = 0; i < robotBlockControlParameters; i++)
                        {
                            logger(AREA, DEBUG, "Going Through Parameter : " + i);

                            int size = 2; // Can also be obtained from the parameters themselves
                            int byte1 = byteOffset + (i * size);
                            int byte2 = byteOffset + (i * size) + 1;

                            byte[] tempBytesForConversion = new byte[2] { memoryBuffer[byte1], memoryBuffer[byte2] };

                            // Need to reverse the bytes to get actual values
                            if (BitConverter.IsLittleEndian)
                            { Array.Reverse(tempBytesForConversion); }

                            // Trigger a new message on rising edge
                            if (i == 0)
                            {

                                if (BitConverter.ToInt16(tempBytesForConversion, 0) == 10 && fleetBlock.Param[0].getValue() == 0)
                                {
                                    logger(AREA, DEBUG, "We've Got A New Message For Robot : " + r);
                                    newMsg = true;
                                    newMsgs[r+1] = true;

                                    updateTaskStatus(r, 10);
                                }
                                else
                                {
                                    //newMsg = false;
                                }
                            }

                            robots[r].Param[i].setValue(BitConverter.ToInt16(tempBytesForConversion));
                            robots[r].Param[i].print();
                        }

                        logger(AREA, DEBUG, "Task No Is: " + robots[0].getTaskNumber());
                        
                        //Console.ReadLine();

/*                        if (robots[r].Param[0].getValue() == TaskStatus.AwaitingPickUp)
                        {
                            newMsg = true;

                            logger(AREA, INFO, "Robot " + r + " Has A New Task. Paramaters: ");

                            for (int i = 0; i < fleetBlockControlParameters; i++)
                            {
                                robots[r].Param[i].print();
                            }

                            updateTaskStatus(r, TaskStatus.TaskReceivedFromPLC);
                        }*/
                    }
                }
                else
                {
                    logger(AREA, ERROR, "Failed to Poll");
                    logger(AREA, ERROR, daveStrerror(memoryres));
                    plcConnectionErrors++;
                    newMsg = false;
                }

            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                plcConnectionErrors++;
                newMsg = false;

                establishConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Polling Failed. Error : ", exception);
                plcConnectionErrors++;
                newMsg = false;
            }
        }
        else
        {
            logger(AREA, INFO, "Simulating Messages From Console");

            if (furtherMsg)
            {
                for (int r = 0; r < noOfRobots; r++)
                {
                    for (int i = 0; i < robotBlockControlParameters+1; i++)
                    {
                        robots[r].Param[i].simulateConsole();
                    }
                }

                newMsg = true;
            }
        }

        logger(AREA, DEBUG, "==== Completed Robot Task Control ====");
    }

    /// <summary>
    /// Writes the PLC fleetBlock Parameters into PLC DB. This is used after a Task was completed by Mirage.
    /// </summary>
    /// <param name="TaskStatus"></param>
    public static void writeFleetBlock(int TaskStatus)
    {
        logger(AREA, DEBUG, "==== Starting To Write Data ====");

        // Copying the Mirage Memory into FleetBlock buffer
        fleetBlock.Param[fleetBlockControlParameters].setValue(TaskStatus);
        fleetMemoryToPLC();

        int result = 1;
        resultSet rs = new resultSet();

        if (live)
        {
            logger(AREA, DEBUG, "Starting A Write Request");

            dataStorageDB = 19;


            PDU p2 = (PDU)dc.prepareWriteRequest();

            //  Go through all the Write Parameters
            //  First, convert all the values to Bytes
            //  Then reverse cause fuck Siemens
            //  Then add to the PDU as a write request
            for (int i = fleetBlockControlParameters; i < fleetBlock.Param.Count; i++)
            {
                fleetBlock.Param[i].print();

                byte[] tempBytes = BitConverter.GetBytes((short)fleetBlock.Param[i].getValue());

                Array.Reverse(tempBytes);

                //if (BitConverter.IsLittleEndian)
                //{ Array.Reverse(tempBytes); }

                p2.addVarToWriteRequest(daveDB, dataStorageDB, fleetBlock.Param[i].getOffset(), fleetBlock.Param[i].getSize(), tempBytes);
            }

            result = dc.execWriteRequest(p2, rs);

            logger(AREA, DEBUG, "Wrote A Request");

            for (int i = 0; i < fleetBlock.Param.Count - fleetBlockControlParameters; i++)
            {
                result = rs.getErrorOfResult(i);
                logger(AREA, DEBUG, "Error Code From: " + fleetBlock.Param[i].getName() + " Code Is: " + result);
            }

            // If no error
            //
        }
        else
        {
            logger(AREA, INFO, "==== Running In Sim Mode ====");
        }
    }

    /// <summary>
    /// Writes the PLC fleetBlock Parameters into PLC DB. This is used after a Task was completed by Mirage.
    /// </summary>
    /// <param name="TaskStatus"></param>
    public static void writeRobotBlock(int robot, int TaskStatus)
    {
        logger(AREA, DEBUG, "==== Starting To Write Data For Robot " + robot + " ====");

        // Copying the Mirage Memory into FleetBlock buffer
        robots[robot].Param[robotBlockControlParameters].setValue(TaskStatus);

        logger(AREA, DEBUG, "Updated Task Status To " + TaskStatus + ". Now Starting to copy memory over");

        robotMemoryToPLC(robot);

        int result = 1;
        resultSet rs = new resultSet();

        if (live)
        {
            PDU p2 = (PDU)dc.prepareWriteRequest();

            //  Go through all the Write Parameters
            //  First, convert all the values to Bytes
            //  Then reverse cause fuck Siemens
            //  Then add to the PDU as a write request
            for (int i = robotBlockControlParameters; i < robots[robot].Param.Count; i++)
            {
                dataStorageDB = 19;

                byte[] tempBytes;

                if (robots[robot].Param[i].getSize() == 2)
                {
                    tempBytes = BitConverter.GetBytes(robots[robot].Param[i].getValue());
                }
                else
                {
                    tempBytes = BitConverter.GetBytes(robots[robot].Param[i].getFloat());
                }

                Array.Reverse(tempBytes);

                //if (BitConverter.IsLittleEndian)
                //{ Array.Reverse(tempBytes); }

                p2.addVarToWriteRequest(daveDB, dataStorageDB, robots[robot].Param[i].getOffset(), robots[robot].Param[i].getSize(), tempBytes);
            }

            result = dc.execWriteRequest(p2, rs);

            for (int i = 0; i < robots[robot].Param.Count; i++)
            {
                result = rs.getErrorOfResult(i);
                logger(AREA, DEBUG, "Error Code From Robot: " + robot + " Param: " + robots[robot].Param[i].getName() + " Code Is: " + result);
            }
        }
        else
        {
            logger(AREA, INFO, "==== Running In Sim Mode ====");
        }
    }

    /// <summary>
    /// Writes the PLC fleetBlock Parameters into PLC DB. This is used after a Task was completed by Mirage.
    /// </summary>
    /// <param name="TaskStatus"></param>
    public static void writeRobotBlock(int robot)
    {
        logger(AREA, DEBUG, "==== Starting To Write Data For Robot " + robot + " ====");

        robotMemoryToPLC(robot);

        int result = 1;
        resultSet rs = new resultSet();

        if (live)
        {
            PDU p2 = (PDU)dc.prepareWriteRequest();

            //  Go through all the Write Parameters
            //  First, convert all the values to Bytes
            //  Then reverse cause fuck Siemens
            //  Then add to the PDU as a write request
            for (int i = robotBlockControlParameters; i < robots[robot].Param.Count; i++)
            {
                //logger(AREA, DEBUG, "Robot: " + i + " Param: " + robots[robot].Param[i].getName() + " Offset: " + robots[robot].Param[i].getOffset());

                dataStorageDB = 19;

                byte[] tempBytes;

                if (robots[robot].Param[i].getSize() == 2)
                {
                    tempBytes = BitConverter.GetBytes(robots[robot].Param[i].getValue());
                }
                else
                {
                    tempBytes = BitConverter.GetBytes(robots[robot].Param[i].getFloat());
                }

                Array.Reverse(tempBytes);

                //if (BitConverter.IsLittleEndian)
                //{ Array.Reverse(tempBytes); }

                p2.addVarToWriteRequest(daveDB, dataStorageDB, robots[robot].Param[i].getOffset(), robots[robot].Param[i].getSize(), tempBytes);
            }

            result = dc.execWriteRequest(p2, rs);

            for (int i = 0; i < robots[robot].Param.Count; i++)
            {
                result = rs.getErrorOfResult(i);
                logger(AREA, DEBUG, "Error Code From Robot: " + robot + " Param: " + robots[robot].Param[i].getName() + " Code Is: " + result);
            }
        }
        else
        {
            logger(AREA, INFO, "==== Running In Sim Mode ====");
        }
    }

    /*
        public static void readDataFromPLC()
        {
            PDU p;
            resultSet rs;
            DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave.libnodave.

            davePrepareReadRequest64(dc, p*);



            davePrepareReadRequest64(dc, &p);
            daveAddVarToReadRequest(&p, daveInputs, 0, 0, 1);
            daveAddVarToReadRequest(&p, daveFlags, 0, 0, 4);
            daveAddVarToReadRequest(&p, daveDB, 6, 20, 2);
            daveAddVarToReadRequest(&p, daveFlags, 0, 12, 2);
            res = dc.daveExecReadRequest(dc, &p, &rs);
        }*/


    /// <summary>
    /// Updates the PLC Task Status memory, in the Task Control Data Block.
    /// </summary>
    /// <param name="status">Designates Task Status Code: still processing, success, failure, etc.</param>
    /// See <see cref="Globals.TaskStatus"/> for possible task status codes.
    public static void updateTaskStatus(int status)
    {
        logger(AREA, DEBUG, "==== Updating Task Status In PLC ====");

        if(live)
        {
            byte[] tempBytes = BitConverter.GetBytes(status);

            if (BitConverter.IsLittleEndian) { Array.Reverse(tempBytes); }

            try
            {
                int result = dc.writeBytes(daveDB, taskControlDB, 12, 4, tempBytes);

                if (result != 0)
                {
                    logger(AREA, ERROR, "Task Status Update Was Unsuccessful");
                    logger(AREA, ERROR, daveStrerror(result));
                    plcConnectionErrors++;
                }
                else
                {
                    logger(AREA, DEBUG, "Task Status Updated To " + status);
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                establishConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed To Write To PLC. Exception: ", exception);
                plcConnectionErrors++;
            }
        }
        else
        {
            logger(AREA, INFO, "==== Running In Sim Mode ====");
        }

        logger(AREA, DEBUG, "==== Update Completed ====");
    }

    /// <summary>
    /// Updates the PLC Task Status memory, in the Task Control Data Block.
    /// </summary>
    /// <param name="status">Designates Task Status Code: still processing, success, failure, etc.</param>
    /// <param name="robot">Designates which robot or fleet to update</param>
    /// See <see cref="Globals.TaskStatus"/> for possible task status codes.
    public static void updateTaskStatus(int robot, int status)
    {
        logger(AREA, DEBUG, "==== Updating Task Status For " + robot + " ====");

        int Task_Status_ID = 4;

        if (live)
        {
            byte[] tempBytes;

            if (robot == fleetID)
            {
                fleetBlock.Param[Task_Status_ID].setValue(status);
                fleetBlock.Param[Task_Status_ID].print();
            }
            else
            {
                robots[robot].Param[Task_Status_ID].setValue(status);
                robots[robot].Param[Task_Status_ID].print();
            }

            tempBytes = BitConverter.GetBytes((short)status);

            logger(AREA, DEBUG, BitConverter.ToString(tempBytes));

            Array.Reverse(tempBytes);

            logger(AREA, DEBUG, BitConverter.ToString(tempBytes));

            logger(AREA, DEBUG, "Bytes Turned into: " + BitConverter.ToInt16(tempBytes));

            try
            {
                int result;

                if (robot==fleetID)
                {
                    result = dc.writeBytes(daveDB, 19, 8, 2, tempBytes);
                    result = dc.writeBytes(daveDB, 19, fleetBlock.Param[Task_Status_ID].getOffset(), fleetBlock.Param[Task_Status_ID].getSize(), tempBytes);
                }
                else
                {
                    result = dc.writeBytes(daveDB, taskControlDB, robots[robot].Param[Task_Status_ID].getOffset(), robots[robot].Param[Task_Status_ID].getSize(), tempBytes);
                }

                if (result != 0)
                {
                    logger(AREA, ERROR, "Task Status Update Was Unsuccessful");
                    logger(AREA, ERROR, daveStrerror(result));
                    plcConnectionErrors++;
                }
                else
                {
                    logger(AREA, DEBUG, "Task Status Updated To " + status);
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                establishConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed To Write To PLC. Exception: ", exception);
                plcConnectionErrors++;
            }
        }
        else
        {
            logger(AREA, INFO, "==== Running In Sim Mode ====");
        }

        logger(AREA, DEBUG, "==== Update Completed ====");
    }

    /// <summary>
    /// Checks to make sure PLC processed and parsed the reponse.
    /// </summary>
    public static void checkResponse()
    {
        logger(AREA, DEBUG, "==== Checking PLC Response ====");

        if (live)
        {
            readFleetHeader();

            readRobots();

            if (fleetBlock.getPLCTaskStatus() == TaskStatus.PlcOK)
            {
                updateTaskStatus(fleetID, TaskStatus.Idle);
            }

            for (int i = 0; i < sizeOfFleet; i++)
            {
                if (robots[i].getPLCTaskStatus() == TaskStatus.PlcOK)
                {
                    updateTaskStatus(i, TaskStatus.Idle);
                }
            }
        }
        else
        {
            logger(AREA, INFO, "==== Running In Sim Mode ====");
            status = TaskStatus.PlcOK;
        }

        // Set message to false as we've processed the message
        newMsg = false;

        logger(AREA, DEBUG, "==== Response Check Completed ====");
    }

    /// <summary>
    /// Reads the alarm block
    /// </summary>
    public static void readAlarms()
    {
        logger(AREA, DEBUG, "==== Reading PLC Alarms ====");

        int alarmOffset = 424;
        int alarmBlockSize = 22;

        if (live)
        {
            int memoryres;
            byte[] memoryBuffer = new byte[alarmBlockSize];

            try
            {
                taskControlDB = 19;

                // readBytes(Area, Data Block Number (in PLC), Start Byte, Length, Byte Container)
                memoryres = dc.readBytes(daveDB, taskControlDB, alarmOffset, alarmBlockSize, memoryBuffer);

                //=========================================================|
                //  Memoryres - return code from Libnodave:                |
                //    0 - Obtained Data                                    |
                //  < 0 - Error detected by Libnodave                      |
                //  > 0 - Error from the PLC                               |
                //=========================================================|
                if (memoryres == 0)
                {
                    logger(AREA, DEBUG, BitConverter.ToString(memoryBuffer));

                    int alarmPosition = 0;

                    for(int i = 0; i < alarmBlockSize/2; i++)
                    {
                        int size = 2;
                        int byte1 = i * size;
                        int byte2 = (i * size) + 1;

                        byte[] tempBytesForConversion = new byte[2] { memoryBuffer[byte1], memoryBuffer[byte2] };

                        BitArray myBA = new BitArray(tempBytesForConversion);

                        for (int a = 0; a < myBA.Count; a++)
                        {
                            plcAlarms.alarm_array[alarmPosition].triggered = myBA[a];
                            //logger(AREA, DEBUG, "Word " + i + " Bit No: " + a + " is set to: " + myBA[a]);

                            alarmPosition++;
                        }
                    }

                    plcAlarms.printAllAlarms();
                }
                else
                {
                    logger(AREA, ERROR, "Failed to Poll");
                    logger(AREA, ERROR, daveStrerror(memoryres));
                    plcConnectionErrors++;
                    newMsg = false;
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                plcConnectionErrors++;
                newMsg = false;

                establishConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Polling Failed. Error : ", exception);
                plcConnectionErrors++;
                newMsg = false;
            }
        }
        else
        {
            logger(AREA, INFO, "Simulating Messages From Console");

            if (!newMsg)
            {
                for (int i = 0; i < fleetBlockControlParameters + 1; i++)
                {
                    fleetBlock.Param[i].simulateConsole();
                }

                string yn;

                Console.WriteLine("Add Robot Commands Further On (y/n)");
                yn = Console.ReadLine();

                if (yn == "y" || yn == "Y")
                {
                    furtherMsg = true;
                }
                else if (yn == "n" || yn == "N")
                {
                    furtherMsg = false;
                }

                newMsg = true;
            }
        }

        logger(AREA, DEBUG, "==== Completed Alarms Read ====");
    }

    /// <summary>
    /// Tries to reconnect to a PLC if the connection drops. 
    /// If it does, and connectivity counter is below 15, it sends an email alert.
    /// </summary>
    public static void checkConnectivity()
    {
        logger(AREA, DEBUG, "==== Checking Connectivity ====");

        if (!plcConnected)
        {
            plcConnectionErrors++;
        }

        if (plcConnectionErrors > 5)
        {
            logger(AREA, WARNING, "Error Counter Passed 5. Trying To Re-establish Connection.");

            // Re-establish connection every fifth loop
            if(plcConnectionErrors % 5 == 0)
            { 
                establishConnection();
            }

            if (plcConnectionErrors == 0)
            {
                logger(AREA, INFO, "Connection Re-established.");
                // We've re-established a connection, error counter resets to 0
            }
            else if (plcConnectionErrors < 15)
            {
                logger(AREA, ERROR, "Failed To Re-establish A Connection. Sending Alerts.");
                // Send an alert
            }
            else
            {
                logger(AREA, ERROR, "Failed To Re-establish A Connection. Alert Has Been Sent.");
            }
        }
        else
        {
            logger(AREA, DEBUG, "Idling");
        }

        logger(AREA, DEBUG, "==== Connectivity Checked ====");
    }

    /// <summary>
    /// Breaks the connection with a PLC. 
    /// </summary>
    public static void disconnect()
    {
        logger(AREA, DEBUG, "==== Disconnecting PLC ====");

        try
        {
            dc.disconnectPLC();
            di.disconnectAdapter();
            closePort(fds.rfd);
        }
        catch (NullReferenceException exception)
        {
            logger(AREA, ERROR, "Connection Has Not Been Instantiated. Exception: ", exception);
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "Failed To Disconnect. The Error Is: ", exception);
        }

        logger(AREA, DEBUG, "==== PLC Disconnection Completed ====");
    }
}
