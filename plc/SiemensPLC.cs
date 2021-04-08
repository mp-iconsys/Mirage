using System;
using System.Xml;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;
using static DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave.libnodave;
using Mirage.plc;
using System.Collections;
using System.Data;

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
    public static int fleetBlockControlParameters = 4;
    public static int fleetBlockSize;
    public static bool furtherMsg;

    public static RobotBlock[] robots;
    public static int noOfRobots;
    public static int robotBlockSize;
    public static int robotBlockControlParameters = 4;

    public static int alarmOffset = 424;
    public static int alarmBlockSize = 22;
    public static Alarms plcAlarms = new Alarms();

    //=========================================================|
    // Watchdog Parameters                                     |
    //=========================================================|
    public static int watchdogFromPLC, watchdogToPLC;
    public static int watchdogFromPLCOffset = 486;
    public static int watchdogToPLCOffset = 484;

    //=========================================================|
    //  Reset Data                                             |
    //=========================================================|
    public static int resetOffset = 478;
    public static int[] resetBits = new int[21];

    //=========================================================|
    //  Message helper parameters                              |
    //=========================================================|
    public static bool[] newMsgs = new bool[10];
    public static bool newMsg;
    public static bool plcConnected;

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

        for (int i = 0; i < sizeOfFleet + 1; i++)
        {
            newMsgs[i] = false;
        }

        int rowcount = 0;

        //==========================================================|
        // First, try to get configuration data from the database   |
        //==========================================================|
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
                    port = int.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "rack")
                {
                    rack = int.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "slot")
                {
                    slot = int.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "taskControlDB")
                {
                    taskControlDB = int.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "dataStorageDB")
                {
                    dataStorageDB = int.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "Live")
                {
                    bool live = bool.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "NoOfRobots")
                {
                    noOfRobots = int.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "RobotBlockSize")
                {
                    robotBlockSize = int.Parse(rdr.GetString(2));
                }
                else if (rdr.GetString(1) == "FleetBlockSIze")
                {
                    fleetBlockSize = int.Parse(rdr.GetString(2));
                }
            }
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "==== Failed To Fetch Config Data From Database ====");
            logger(AREA, ERROR, "Exception: ", e);
        }

        //==========================================================|
        // If we don't have all the configs from the database       | 
        // read from the XML config file                            |
        //==========================================================|
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

                logger(AREA, DEBUG, "IP : " + IP);
                logger(AREA, DEBUG, "Port : " + port);
                logger(AREA, DEBUG, "Rack : " + rack);
                logger(AREA, DEBUG, "Slot : " + slot);
                logger(AREA, DEBUG, "Task Control DB No : " + taskControlDB);
                logger(AREA, DEBUG, "Data Storage DB No : " + dataStorageDB);
            }
            catch (Exception exception)
            {
                keepRunning = false;
                logger(AREA, ERROR, "Failed to load PLC configuration file. Mirage will terminate. Exception: ", exception);
            }
        }

        logger(AREA, DEBUG, "==== Getting Robot Area Block Structure ====");

        rowcount = 0;

        //==========================================================|
        // Alright, Now we're going to set up the corresponding     |
        // data blocks for talking with the PLC                     |
        //==========================================================|
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
                    int param_size = int.Parse(rdr.GetString(2));
                    int param_offset = fleetBlock.Offset;

                    if (rdr.GetString(3) == "INT")
                    {
                        if (rowcount != 0)
                        {
                            //==================================================================|       
                            // The offset for the current parameter is equal to:                |
                            // Robot Offset + Previous Param Offset + Previous Param Size       |
                            //==================================================================| 
                            param_offset = fleetBlock.Param[rowcount - 1].getOffset() + fleetBlock.Param[rowcount - 1].getSize();
                        }

                        Parameter_INT temp = new Parameter_INT(param_name, param_size, param_offset);

                        fleetBlock.Param.Add(temp);
                    }
                    else if (rdr.GetString(3) == "FLOAT")
                    {
                        if (rowcount != 0)
                        {
                            //==================================================================|       
                            // The offset for the current parameter is equal to:                |
                            // Robot Offset + Previous Param Offset + Previous Param Size       |
                            //==================================================================| 
                            param_offset = fleetBlock.Param[rowcount - 1].getOffset() + fleetBlock.Param[rowcount - 1].getSize();
                        }

                        Parameter_FLOAT temp = new Parameter_FLOAT(param_name, param_size, param_offset);

                        fleetBlock.Param.Add(temp);
                    }

                    rowcount++;
                }
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Initialize Fleet Data Block");
                logger(AREA, ERROR, "Exception is: ", e);
            }

            //=========================================================|
            //  Instantiate robot block array                          |
            //=========================================================|
            robots = new RobotBlock[noOfRobots];

            for (int i = 0; i < noOfRobots; i++)
            {
                // We're not interested in Robot 0 so start reading at robot 1
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
                        int param_size = int.Parse(rdr.GetString(2));
                        int param_offset = robots[i].Offset;

                        if (rdr.GetString(3) == "INT")
                        {
                            if (rowcount != 0)
                            {
                                //==================================================================|       
                                // The offset for the current parameter is equal to:                |
                                // Robot Offset + Previous Param Offset + Previous Param Size       |
                                //==================================================================| 
                                param_offset = robots[i].Param[rowcount - 1].getOffset() + robots[i].Param[rowcount - 1].getSize();
                            }

                            Parameter_INT temp = new Parameter_INT(param_name, param_size, param_offset);

                            robots[i].Param.Add(temp);
                        }
                        else if (rdr.GetString(3) == "FLOAT")
                        {
                            if (rowcount != 0)
                            {
                                //==================================================================|       
                                // The offset for the current parameter is equal to:                |
                                // Robot Offset + Previous Param Offset + Previous Param Size       |
                                //==================================================================| 
                                param_offset = robots[i].Param[rowcount - 1].getOffset() + robots[i].Param[rowcount - 1].getSize();
                            }

                            Parameter_FLOAT temp = new Parameter_FLOAT(param_name, param_size, param_offset);

                            robots[i].Param.Add(temp);
                        }
                    }

                    rowcount++;
                }
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Initialize Robot Data Blocks");
                logger(AREA, ERROR, "Exception is: ", e);
            }
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "Failed To Fetch PLC Data Block Information From Database");
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
            for (int j = 0; j < robots[0].Param.Count; j++)
            {
                robots[i].Param[j].print();
            }
        }

        logger(AREA, INFO, "PLC Initialization Completed");
    }

    /// <summary>
    /// Connects to a Siemens PLC. Based on information obtained through the initialize function.
    /// </summary>
    public static void establishConnection()
    {
        logger(AREA, DEBUG, "==== Establishing A Connection ====");

        try
        {
            fds.rfd = openSocket(port, IP);
            fds.wfd = fds.rfd;
        }
        catch (Exception e)
        {
            logger(AREA, ERROR, "Failed To Open Libnodave Socket");
            logger(AREA, ERROR, "Exception: ", e);
        }

        if (fds.rfd != IntPtr.Zero)
        {
            logger(AREA, DEBUG, "Socket Opened Successfully");

            try
            {
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
                }
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Connect To The PLC");
                logger(AREA, ERROR, "Exception: ", e);
            }
        }
        else
        {
            logger(AREA, ERROR, "Socket Failed To Open. DaveOSserialType is initialized to " + fds.rfd);
            logger(AREA, ERROR, daveStrerror(fds.rfd.ToInt32()));

            plcConnectionErrors++;
            plcConnected = false;
        }

        logger(AREA, DEBUG, "==== Established A Connection ====");
    }

    /// <summary>
    /// Polls the PLC for data regarding new tasks or missions.
    /// </summary>
    public static void poll()
    {
        logger(AREA, DEBUG, "Polling The PLC");

        readFleetHeader();

        readRobots();

        logger(AREA, DEBUG, "Completed Polling");
    }

    /// <summary>
    /// 
    /// </summary>
    public static void readFleetHeader()
    {
        logger(AREA, DEBUG, "==== Reading Fleet Header====");

        int memoryres = 1;
        byte[] memoryBuffer = new byte[fleetBlockControlParameters * 2];

        if (plcConnected)
        {
            try
            {
                taskControlDB = 19;

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

                        if (i == 0)
                        {
                            //Used to be: fleetBlock.Param[0].getValue() == 0
                            //Now is: fleetBlock.getTaskStatus()
                            // BitConverter.ToInt16(tempBytesForConversion, 0) == 10 && fleetBlock.getTaskStatus() == 0
                            if (BitConverter.ToInt16(tempBytesForConversion, 0) == 10 && fleetBlock.getTaskStatus() == 0)
                            {
                                logger(AREA, INFO, "Got A Mission For Fleet");

                                for (int j = 0; j < fleetBlockControlParameters; j++)
                                {
                                    fleetBlock.Param[j].print();
                                }

                                updateTaskStatus(fleetID, 10);
                                newMsgs[0] = true;

                                logger(AREA, INFO, "New Message Is: " + newMsgs[0].ToString());
                                newMsg = true;
                            }
                            else
                            {
                                logger(AREA, DEBUG, "PLC Fleet Block Idle");
                                //newMsgs[0] = false;
                            }
                        }

                        fleetBlock.Param[i].setValue(BitConverter.ToInt16(tempBytesForConversion, 0));
                    }
                }
                else
                {
                    logger(AREA, WARNING, "Failed to read PLC in readFleetHeader()");
                    logger(AREA, WARNING, daveStrerror(memoryres));
                    restartConnection();
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                restartConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Polling Failed. Error : ", exception);
                restartConnection();
            }
        }
        else
        {
            logger(AREA, ERROR, "Not Connected To The PLC During Fleet Header Read. Trying to re-establish connection");
            restartConnection();
        }

        logger(AREA, DEBUG, "==== Completed Fleet Header ====");
    }

    /// <summary>
    /// 
    /// </summary>
    public static void readRobots()
    {
        logger(AREA, DEBUG, "==== Reading Robot Task Control ====");

        if (plcConnected)
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

                //================================================================| 
                // For each robot, go through the first (control) parameters.     |
                // Read them and save the data in the robotBlock structure.       |
                // If any TaskStatus is 10, that means we've got tasks to do.     |
                //================================================================|
                for (int r = 0; r < noOfRobots; r++)
                {
                    // readBytes(Area, Data Block Number (in PLC), Start Byte, Length, Byte Container)
                    memoryres = dc.readBytes(daveDB, taskControlDB, robots[r].Offset, robotBlockControlParameters * 2, memoryBuffer);

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

                        logger(AREA, DEBUG, "Going Through Robot : " + r);

                        //int byteOffset = robots[r].Offset;
                        // Robot Control Parameters, each is 2 bytes
                        //int byteOffset = r * robotBlockControlParameters * 2;
                        int byteOffset = 0;//

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
                                //logger(AREA, INFO, "NEW PLC Task Status: " + BitConverter.ToInt16(tempBytesForConversion, 0) + " OLD PLC Task Status: " + robots[r].getTaskStatus());
                                //robots[0].getPLCTaskStatus()
                                //robots[r].Param[0].getValue() == 0
                                if (BitConverter.ToInt16(tempBytesForConversion, 0) == 10 && robots[r].getTaskStatus() == 0)
                                {
                                    logger(AREA, INFO, "NEW MESSAGE FOR ROBOT : " + r);

                                    newMsg = true;
                                    newMsgs[r + 1] = true;

                                    updateTaskStatus(r, TaskStatus.StartedProcessing);
                                }
                            }

                            robots[r].Param[i].setValue(BitConverter.ToInt16(tempBytesForConversion));
                            robots[r].Param[i].print();
                        }

                        logger(AREA, DEBUG, "Task No Is: " + robots[r].getTaskNumber());
                    }
                    else
                    {
                        logger(AREA, ERROR, "Failed to Poll in readRobots()");
                        logger(AREA, ERROR, daveStrerror(memoryres));
                        restartConnection();
                    }
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                restartConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Polling Failed. Error : ", exception);
                restartConnection();
            }
        }
        else
        {
            logger(AREA, ERROR, "Not Connected To The PLC During Robot Header Read. Trying to re-establish connection");
            restartConnection();
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

        if (plcConnected)
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

            try
            {
                result = dc.execWriteRequest(p2, rs);
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Write To PLC in writeFleetBlock()");
                logger(AREA, ERROR, "Exception: ", e);
                restartConnection();
            }

            logger(AREA, DEBUG, "Wrote A Request");

            /*            for (int i = 0; i < fleetBlock.Param.Count - fleetBlockControlParameters; i++)
                        {
                            result = rs.getErrorOfResult(i);
                            logger(AREA, DEBUG, "Error Code From: " + fleetBlock.Param[i].getName() + " Code Is: " + result);
                        }
            */
        }
        else
        {
            logger(AREA, ERROR, "Cannot Write To Fleet Block As The PLC Is Not Connected");
            restartConnection();
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

        if (plcConnected)
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
            logger(AREA, ERROR, "Cannot Write To Fleet Block As The PLC Is Not Connected");
            restartConnection();
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

        if (plcConnected)
        {
            PDU p2 = (PDU)dc.prepareWriteRequest();

            logger(AREA, DEBUG, "Writing Robot Data For Robot " + robot);

            //  Go through all the Write Parameters
            //  First, convert all the values to Bytes
            //  Then reverse cause fuck Siemens
            //  Then add to the PDU as a write request
            for (int i = robotBlockControlParameters; i < robots[robot].Param.Count; i++)
            {
                //logger(AREA, DEBUG, "Robot: " + i + " Param: " + robots[robot].Param[i].getName() + " Offset: " + robots[robot].Param[i].getOffset());

                byte[] tempBytes;

                if (robots[robot].Param[i].getSize() == 2)
                {
                    tempBytes = BitConverter.GetBytes(robots[robot].Param[i].getValue());
                    logger(AREA, DEBUG, " Param: " + robots[robot].Param[i].getName() + " Value: " + robots[robot].Param[i].getValue()); // For Debugging
                }
                else
                {
                    tempBytes = BitConverter.GetBytes(robots[robot].Param[i].getFloat());
                    logger(AREA, DEBUG, " Param: " + robots[robot].Param[i].getName() + " Value: " + robots[robot].Param[i].getFloat()); // For Debugging
                }

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(tempBytes);
                }

                p2.addVarToWriteRequest(daveDB, dataStorageDB, robots[robot].Param[i].getOffset(), robots[robot].Param[i].getSize(), tempBytes);
            }

            try
            {
                result = dc.execWriteRequest(p2, rs);
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed to Write Exception in writeRobotBlock()", e);
                restartConnection();
            }

            /*
            for (int i = 0; i < robots[robot].Param.Count; i++)
            {
                result = rs.getErrorOfResult(i);
                logger(AREA, DEBUG, "Error Code From Robot: " + robot + " Param: " + robots[robot].Param[i].getName() + " Code Is: " + result);
            }*/
        }
        else
        {
            logger(AREA, ERROR, "Cannot Write To Fleet Block As The PLC Is Not Connected");
            restartConnection();
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
    /// Helper method that prints the message and task status.
    /// Only used for debugging - no functional purpose
    /// </summary>
    public static void printNewMessageStatus()
    {
        logger(AREA, DEBUG, "Fleet Mirage Task Status: " + fleetBlock.getTaskStatus());
        logger(AREA, DEBUG, "Fleet PLC Task Status: " + fleetBlock.getPLCTaskStatus());
        logger(AREA, DEBUG, "Fleet Message Status: " + newMsgs[0].ToString());

        for (int g = 1; g < sizeOfFleet + 1; g++)
        {
            logger(AREA, DEBUG, "Robot " + (g - 1) + " Mirage Task Status: " + robots[g - 1].getTaskStatus());
            logger(AREA, DEBUG, "Robot " + (g - 1) + " Mission Status (Memory): " + mirFleet.robots[g - 1].schedule.state_id);
            logger(AREA, DEBUG, "Robot " + (g - 1) + " Mission Status (PLC Buffer): " + robots[g - 1].Param[5].getValue());
            logger(AREA, DEBUG, "Robot " + (g - 1) + " PLC Task Status: " + robots[g - 1].getPLCTaskStatus());
            logger(AREA, DEBUG, "Robot " + (g - 1) + " Message Status: " + newMsgs[g].ToString());
        }
    }

    /// <summary>
    /// Updates the PLC Task Status memory, in the Task Control Data Block.
    /// </summary>
    /// <param name="status">Designates Task Status Code: still processing, success, failure, etc.</param>
    /// <param name="robot">Designates which robot or fleet to update</param>
    /// See <see cref="Globals.TaskStatus"/> for possible task status codes.
    public static void updateTaskStatus(int robot, int status)
    {
        int Task_Status_ID = 4;

        if (plcConnected)
        {
            byte[] tempBytes;

            if (robot == fleetID)
            {
                logger(AREA, DEBUG, "Updating Task Status For Fleet To " + status);

                fleetBlock.Param[Task_Status_ID].setValue(status);
                fleetBlock.Param[Task_Status_ID].print();
            }
            else
            {
                logger(AREA, DEBUG, "Updating Task Status For Robot " + robot + " To " + status);

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

                if (robot == fleetID)
                {
                    result = dc.writeBytes(daveDB, 19, fleetBlock.Param[Task_Status_ID].getOffset(), fleetBlock.Param[Task_Status_ID].getSize(), tempBytes);
                }
                else
                {
                    result = dc.writeBytes(daveDB, taskControlDB, robots[robot].Param[Task_Status_ID].getOffset(), robots[robot].Param[Task_Status_ID].getSize(), tempBytes);
                }

                if (result != 0)
                {
                    logger(AREA, ERROR, "Task Status Update Was Unsuccessful: " + daveStrerror(result));
                    restartConnection();
                }
                else
                {
                    logger(AREA, DEBUG, "Task Status Was Updated Sucessfully");
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                restartConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed To Write To PLC. Exception: ", exception);
                restartConnection();
            }
        }
        else
        {
            logger(AREA, ERROR, "Cannot Update Task Status As The PLC Is Not Connected");
            restartConnection();
        }

        logger(AREA, DEBUG, "==== Update Completed ====");
    }

    /// <summary>
    /// Sets a single bit in the PLC to the high value (1). This is done to reset
    /// a sequence or reset the tote count that's on top of the robot
    /// </summary>
    /// <param name="resetID">The ID of the parameter in the PLC block</param>
    public static void updateResetBits(int resetID)
    {
        //=========================================================|
        // The bit we want to reset is:                            |
        // (No of bytes) * (Size of a byte) + Bit position         |
        //=========================================================|
        int startBit = resetOffset * 8 + resetID;

        logger(AREA, INFO, "Reseting Sequence ID: " + resetID);
        logger(AREA, INFO, "Start Bit Is:  " + startBit);

        if (plcConnected)
        {
            byte[] tempBytes = new byte[] { 255 };

            // TODO: check if needed - probablt not since 255 is 11111111
            Array.Reverse(tempBytes);

            try
            {
                int result;

                result = dc.writeBits(daveDB, 19, startBit, 1, tempBytes);

                if (result != 0)
                {
                    logger(AREA, ERROR, "Sequence Reset Was Unsuccessful");
                    logger(AREA, ERROR, daveStrerror(result));
                    restartConnection();
                }
                else
                {
                    logger(AREA, DEBUG, "Sequence Was Reset Successfully");
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                restartConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed To Write To PLC. Exception: ", exception);
                restartConnection();
            }
        }
        else
        {
            logger(AREA, ERROR, "Cannot Send Reset Bits As The PLC Is Not Connected");
            restartConnection();
        }
    }

    /// <summary>
    /// Checks to make sure PLC processed and parsed the reponse.
    /// </summary>
    public static void checkPLCResponse()
    {
        logger(AREA, DEBUG, "Checking PLC Response");

        if (plcConnected)
        {
            readFleetHeader();

            readRobots();

            // Used to be: if (fleetBlock.getPLCTaskStatus() == TaskStatus.PlcIdle)
            // Changed to: if (fleetBlock.getPLCTaskStatus() == TaskStatus.PlcIdle && fleetBlock.getTaskStatus() != TaskStatus.PlcIdle)
            // This should only write to the PLC on the falling edge
            // Can't be too smart as we need to make sure we overwrite the mission status to 0 when it completes the mission
            if (fleetBlock.getPLCTaskStatus() == TaskStatus.PlcIdle && fleetBlock.getTaskStatus() != TaskStatus.Idle)
            {
                logger(AREA, INFO, "Resetting Task Status And Fleet Return Parameter To 0 (Idle) From " + fleetBlock.getTaskStatus());

                mirFleet.returnParameter = 0;
                updateTaskStatus(fleetID, TaskStatus.Idle);
            }

            for (int robotID = 0; robotID < sizeOfFleet; robotID++)
            {
                // Used to be: if (robots[robotID].getPLCTaskStatus() == TaskStatus.PlcIdle)
                // Changed to: if (robots[robotID].getPLCTaskStatus() == TaskStatus.PlcIdle && robots[robotID].getTaskStatus() != TaskStatus.PlcIdle)
                // This should only write to the PLC on the falling edge
                // Can't be too smart as we need to make sure we overwrite the mission status to 0 when it completes the mission
                if (robots[robotID].getPLCTaskStatus() == TaskStatus.PlcIdle && (robots[robotID].getTaskStatus() != TaskStatus.PlcIdle))
                {
                    logger(AREA, INFO, "Resetting Task And Mission Status To 0 (Idle) For Robot : " + robotID);

                    // This should already be 0 from the read we did at the top
                    mirFleet.robots[robotID].schedule.state_id = TaskStatus.Idle;
                    robots[robotID].setTaskStatus(TaskStatus.Idle);

                    // Added as new feature:
                    mirFleet.robots[robotID].schedule.id = 0;

                    // TODO: Uncommented for now - check if it needs to be uncommented on 06/04/2021
                    // robotMemoryToPLC(robotID);
                    // writeRobotBlock(robotID);
                }
                else if (robots[robotID].getPLCTaskStatus() == TaskStatus.PlcIdle && (mirFleet.robots[robotID].schedule.state_id == TaskStatus.CompletedNoErrors))
                {
                    mirFleet.robots[robotID].schedule.state_id = TaskStatus.Idle;
                    robots[robotID].setTaskStatus(TaskStatus.Idle);
                    //mirFleet.robots[robotID].schedule.state_id = TaskStatus.Idle;

                    //updateTaskStatus(robotID, TaskStatus.Idle);

                    // TODO: not needed since we write to PLC at the end anyway?
                    robotMemoryToPLC(robotID);
                    //writeRobotBlock(robotID);
                }
                else if (robots[robotID].getPLCTaskStatus() == TaskStatus.PlcIdle && mirFleet.robots[robotID].schedule.state_id == TaskStatus.CouldntProcessRequest)
                {
                    mirFleet.robots[robotID].schedule.state_id = TaskStatus.Idle;
                    robots[robotID].setTaskStatus(TaskStatus.Idle);
                    //mirFleet.robots[robotID].schedule.state_id = TaskStatus.Idle;

                    //updateTaskStatus(robotID, TaskStatus.Idle);

                    // TODO: not needed since we write to PLC at the end anyway?
                    robotMemoryToPLC(robotID);
                    //writeRobotBlock(robotID);
                }

                // If PLC is Idle AND:
                // - Our Task Status is not idle
                // - Or Our Mission Status is either complete or failed
                if (robots[robotID].getPLCTaskStatus() == TaskStatus.PlcIdle
                  && (robots[robotID].getTaskStatus() != TaskStatus.PlcIdle
                  || mirFleet.robots[robotID].schedule.state_id == TaskStatus.CompletedNoErrors
                  || mirFleet.robots[robotID].schedule.state_id == TaskStatus.CouldntProcessRequest))
                {
                    logger(AREA, INFO, "Resetting Task And Mission Status To 0 (Idle) For Robot : " + robotID);

                    // This should already be 0 from the read we did at the top
                    mirFleet.robots[robotID].schedule.state_id = TaskStatus.Idle;
                    robots[robotID].setTaskStatus(TaskStatus.Idle);

                    // Added as new feature:
                    mirFleet.robots[robotID].schedule.id = 0;
                    robotMemoryToPLC(robotID);
                }
            }
        }
        else
        {
            logger(AREA, ERROR, "Cannot Check PLC Idle/Sending/Processing As the PLC is Not Connected");
            establishConnection();
        }

        logger(AREA, DEBUG, "Response Check Completed");
    }

    /// <summary>
    /// Reads the PLC Alarms block and checks if any are triggered. 
    /// If they are, it saves the rising and falling edge and records in the DB.
    /// </summary>
    public static void readAlarms()
    {
        logger(AREA, DEBUG, "Reading PLC Alarms");

        if (plcConnected)
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

                    for (int i = 0; i < alarmBlockSize / 2; i++)
                    {
                        int size = 2;
                        int byte1 = i * size;
                        int byte2 = (i * size) + 1;

                        byte[] tempBytesForConversion = new byte[2] { memoryBuffer[byte1], memoryBuffer[byte2] };

                        BitArray myBA = new BitArray(tempBytesForConversion);

                        for (int a = 0; a < myBA.Count; a++)
                        {
                            plcAlarms.alarm_array[alarmPosition].triggered = myBA[a];
                            alarmPosition++;
                        }
                    }

                    //plcAlarms.printAllAlarms();
                    plcAlarms.checkAlarms();
                }
                else
                {
                    logger(AREA, ERROR, "Failed to Poll PLC For Alarms");
                    logger(AREA, ERROR, daveStrerror(memoryres));
                    restartConnection();
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, WARNING, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                logger(AREA, WARNING, "Trying To Establish Connection Again");
                restartConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "PLC Polling Failed. Error : ", exception);
                restartConnection();
            }
        }
        else
        {
            logger(AREA, ERROR, "Cannot Read Alarms As the PLC is Not Connected");
            restartConnection();
        }

        logger(AREA, DEBUG, "Completed Reading PLC Alarms");
    }

    /// <summary>
    /// Tries to reconnect to a PLC if the connection drops. 
    /// If it does, and connectivity counter is below 15, it sends an email alert.
    /// </summary>
    public static void checkConnectivity()
    {
        logger(AREA, DEBUG, "Checking PLC Connectivity");

        if (!plcConnected)
        {
            restartConnection();
        }

        if (plcConnectionErrors > 5)
        {
            logger(AREA, WARNING, "Error Counter Passed 5. Trying To Re-establish Connection.");

            // Re-establish connection every fifth loop
            if (plcConnectionErrors % 5 == 0)
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
            logger(AREA, DEBUG, "Connected To The PLC");
        }

        updateWatchdog();

        logger(AREA, DEBUG, "PLC Connectivity Checked");
    }

    /// <summary>
    /// Scans the PLC watchdog, iterates the number by 1 and writes back.
    /// </summary>
    private static void updateWatchdog()
    {
        logger(AREA, DEBUG, "Updating PLC Watchdog");

        int memoryres = 1;
        int watchdogSize = 2;
        int watchdogMaxValue = 32767;
        byte[] memoryBuffer = new byte[watchdogSize];

        //==========================================================|
        // First, get the PLC Watchdog integer                      |
        //==========================================================|
        if (plcConnected)
        {
            try
            {
                memoryres = dc.readBytes(daveDB, taskControlDB, watchdogFromPLCOffset, watchdogSize, memoryBuffer);

                if (memoryres == 0)
                {
                    logger(AREA, DEBUG, BitConverter.ToString(memoryBuffer));

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(memoryBuffer);
                    }

                    watchdogFromPLC = BitConverter.ToInt16(memoryBuffer);

                    if (watchdogFromPLC <= watchdogMaxValue)
                    {
                        watchdogToPLC = watchdogFromPLC;
                    }
                    else
                    {
                        logger(AREA, WARNING, "PLC Watchdig Exceeded Maximum Value For Signed Int16. Resetting To 0");
                        watchdogToPLC = 0;
                    }

                    updateWatchdogInDB(watchdogFromPLC);
                    //logger(AREA, INFO, "Watchdog: " + watchdogFromPLC);
                }
                else
                {
                    logger(AREA, WARNING, "Failed to read PLC in SiemensPLC.updateWatchdog");
                    logger(AREA, WARNING, daveStrerror(memoryres));

                    restartConnection();
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                //plcConnectionErrors++;
                restartConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Polling Failed. Error : ", exception);
                //plcConnectionErrors++;
                restartConnection();
            }

            if (plcConnected)
            {
                memoryBuffer = BitConverter.GetBytes((short)watchdogToPLC);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(memoryBuffer);
                }

                logger(AREA, DEBUG, "Return Watchdog Parameter: " + BitConverter.ToString(memoryBuffer));

                try
                {
                    int result = 999;

                    result = dc.writeBytes(daveDB, taskControlDB, watchdogToPLCOffset, watchdogSize, memoryBuffer);

                    if (result != 0)
                    {
                        logger(AREA, ERROR, "Failed To Update Watchdog");
                        logger(AREA, ERROR, daveStrerror(result));
                        restartConnection();
                    }
                    else
                    {
                        logger(AREA, DEBUG, "Task Status Updated To " + status);
                    }
                }
                catch (NullReferenceException exception)
                {
                    logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                    restartConnection();
                }
                catch (Exception exception)
                {
                    logger(AREA, ERROR, "Failed To Write To PLC. Exception: ", exception);
                    restartConnection();
                }
            }
        }
        else
        {
            logger(AREA, ERROR, "Cannot Update PLC Watchdog As the PLC is Not Connected");
            restartConnection();
        }
    }

    private static void updateWatchdogInDB(int newWatchdog)
    {
        try
        {
            MySqlCommand cmd = new MySqlCommand();
            cmd.Connection = db;
            cmd.CommandText = "store_plc_watchdog";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.AddWithValue("@Watchdog", newWatchdog);
            cmd.Parameters["@Watchdog"].Direction = ParameterDirection.Input;

            cmd.ExecuteNonQuery();
            cmd.Dispose();
        }
        catch (Exception exception)
        {
            logger(AREA, ERROR, "MySQL Query Error: ", exception);
        }
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


    private static void restartConnection()
    {
        logger(AREA, WARNING, "Restarting Connection");

        disconnect();

        establishConnection();
    }
}
