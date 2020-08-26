using System;
using System.Xml;
using static Globals;
using static Globals.DebugLevel;
using static DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave.libnodave;

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
        //  Message helper parameters                              |
        //=========================================================|
        public static bool newMsg;

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        public static int plcConnectionErrors = 0;
        private static readonly Type AREA = typeof(SiemensPLC);

        /// <summary>
        /// Opens the plc.config file and sets static variables such as IP.
        /// </summary>
        public static void initialize()
        {
            logger(AREA, DEBUG, "==== Starting Initialization ====");

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
                logger(AREA, ERROR, "Siemens PLC failed to load configuration file. Mirage will terminate. Exception : ", exception);
            }

            logger(AREA, INFO, "==== Initialization Completed ====");
        }

        /// <summary>
        /// Connects to a Siemens PLC. Based on information obtained through the initialize function.
        /// </summary>
        public static void establishConnection()
        {
            logger(AREA, DEBUG, "==== Establishing A Connection ====");

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
                    plcConnectionErrors = 0; // Reset counter on successfull connection
                }
                else
                {
                    logger(AREA, ERROR, "Failed To Connect. Trying again, with result " + dc.connectPLC());
                    logger(AREA, ERROR, daveStrerror(dc.connectPLC()));

                    plcConnectionErrors++;
                    newMsg = false;
                }
            }
            else
            {
                logger(AREA, ERROR, "Socket Failed To Open. DaveOSserialType is initialized to " + fds.rfd);
                logger(AREA, ERROR, daveStrerror(fds.rfd.ToInt32()));

                plcConnectionErrors++;
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

            int serialNumber, task, robotID, status, parameter, memoryres;
            byte[] memoryBuffer = new byte[20];

            try
            {
                // readBytes(Area, Data Block Number (in PLC), Start Byte, Length, Byte Container)
                memoryres = dc.readBytes(daveDB, taskControlDB, 0, 20, memoryBuffer);

                if (memoryres == 0)
                {
                    logger(AREA, DEBUG, BitConverter.ToString(memoryBuffer));

                    byte[] tempBytesForConversion = new byte[4] { memoryBuffer[0], memoryBuffer[1], memoryBuffer[2], memoryBuffer[3] };

                    // Need to reverse the bytes to get actual values
                    if (BitConverter.IsLittleEndian) { Array.Reverse(tempBytesForConversion); }

                    serialNumber = BitConverter.ToInt32(tempBytesForConversion, 0);

                    // Only change memory if the PLC is making a new request
                    if (serialNumber != SiemensPLC.serialNumber)
                    {
                        // This determines which MiR to affect
                        tempBytesForConversion = new byte[4] { memoryBuffer[4], memoryBuffer[5], memoryBuffer[6], memoryBuffer[7] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempBytesForConversion);
                        robotID = BitConverter.ToInt32(tempBytesForConversion, 0);

                        // This determines which action to perform
                        tempBytesForConversion = new byte[4] { memoryBuffer[8], memoryBuffer[9], memoryBuffer[10], memoryBuffer[11] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempBytesForConversion);
                        task = BitConverter.ToInt32(tempBytesForConversion, 0);

                        // This is the status of the request - should be 0
                        tempBytesForConversion = new byte[4] { memoryBuffer[12], memoryBuffer[13], memoryBuffer[14], memoryBuffer[15] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempBytesForConversion);
                        status = BitConverter.ToInt32(tempBytesForConversion, 0);

                        // Additional data, such as mission number, etc
                        tempBytesForConversion = new byte[4] { memoryBuffer[16], memoryBuffer[17], memoryBuffer[18], memoryBuffer[19] };
                        if (BitConverter.IsLittleEndian)
                            Array.Reverse(tempBytesForConversion);
                        parameter = BitConverter.ToInt32(tempBytesForConversion, 0);

                        newMsg = true;
                        SiemensPLC.status = status;
                        SiemensPLC.task = task;
                        SiemensPLC.robotID = robotID;
                        SiemensPLC.serialNumber = serialNumber;
                        SiemensPLC.parameter = parameter;

                        logger(AREA, DEBUG, "Status : " + status);
                        logger(AREA, DEBUG, "Task : " + task);
                        logger(AREA, DEBUG, "Robot ID : " + robotID);
                        logger(AREA, DEBUG, "Serial No : " + serialNumber);
                        logger(AREA, DEBUG, "Parameter : " + parameter);
                    }
                    else
                    {
                        newMsg = false;
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

            // Added for testing messages
/*            if(!newMsg)
            {
                Console.WriteLine("Enter SerialNumber: ");
                SiemensPLC.serialNumber = Int32.Parse(Console.ReadLine());

                Console.WriteLine("Enter Task: ");
                SiemensPLC.task = Int32.Parse(Console.ReadLine());

                Console.WriteLine("Enter Robot ID: ");
                SiemensPLC.robotID = Int32.Parse(Console.ReadLine());

                SiemensPLC.status = 0;
                SiemensPLC.parameter = 0;
                SiemensPLC.newMsg = true;
            }*/

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
            logger(AREA, DEBUG, "Type : " + type);
            logger(AREA, DEBUG, "Status Code : " + statusCode);
            logger(AREA, DEBUG, "Data : " + data);

            int result = 1;

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
                else if(type == "battery")
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
            else if(statusCode == TaskStatus.CouldntProcessRequest)
            {
                logger(AREA, WARNING, "We Couldn't Process The Request. Status Code : " + statusCode);
                updateTaskStatus(TaskStatus.CouldntProcessRequest);
            }
            else if(statusCode == TaskStatus.FatalError)
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
            logger(AREA, DEBUG, "Type : " + type);
            logger(AREA, DEBUG, "Status Code : " + statusCode);
            logger(AREA, DEBUG, "Data : " + data);

            int result = 1;

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

            logger(AREA, INFO, "==== Completed Data Write ====");
        }

        /// <summary>
        /// Updates the PLC Task Status memory, in the Task Control Data Block.
        /// </summary>
        /// <param name="status">Designates Task Status Code: still processing, success, failure, etc.</param>
        /// See <see cref="Globals.TaskStatus"/> for possible task status codes.
        public static void updateTaskStatus(int status)
        {
            logger(AREA, DEBUG, "==== Updating Task Status In PLC ====");

            byte[] tempBytes = BitConverter.GetBytes(status);

            if (BitConverter.IsLittleEndian) { Array.Reverse(tempBytes); }

            try
            {
                int result = dc.writeBytes(daveDB, taskControlDB, 12, 4, tempBytes);

                if(result != 0)
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
            catch(NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                establishConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed To Write To PLC. Exception: ", exception);
                plcConnectionErrors++;
            }
            finally
            {

            }

            logger(AREA, DEBUG, "==== Update Completed ====");
        }

        /// <summary>
        /// Checks to make sure PLC processed and parsed the reponse.
        /// </summary>
        public static void checkResponse()
        {
            logger(AREA, DEBUG, "==== Checking PLC Response ====");

            byte[] tempByteBuffer = new byte[4];
            int memoryres = 1;

            try
            {
                memoryres = dc.readBytes(daveDB, taskControlDB, 12, 4, tempByteBuffer);

                // PLC read was successful
                if (memoryres == 0)
                {
                    status = BitConverter.ToInt32(tempByteBuffer, 0);

                    if (status == TaskStatus.PlcOK)
                    {
                        logger(AREA, DEBUG, "PLC Processed Data");
                    }
                    else if (status == TaskStatus.PlcError)
                    {
                        logger(AREA, ERROR, "PLC Failed To Process Data");
                    }
                    else
                    {
                        logger(AREA, ERROR, "Unknown Status");
                    }
                }
                else
                {
                    logger(AREA, ERROR, "Response Check Failed");
                    logger(AREA, ERROR, daveStrerror(memoryres));
                    plcConnectionErrors++;
                }
            }
            catch (NullReferenceException exception)
            {
                logger(AREA, ERROR, "Dave Connection Has Not Been Instantiated. Exception: ", exception);
                plcConnectionErrors++;
                establishConnection();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "Failed To Fetch Response Data: ", exception);
                plcConnectionErrors++;
            }
            finally
            {

            }

            // Set message to false as we've processed the message
            newMsg = false;

            logger(AREA, DEBUG, "==== Response Check Completed ====");
        }

        /// <summary>
        /// Tries to reconnect to a PLC if the connection drops. 
        /// If it does, and connectivity counter is below 15, it sends an email alert.
        /// </summary>
        public static void checkConnectivity()
        {
            logger(AREA, DEBUG, "==== Checking Connectivity ====");

            if (plcConnectionErrors > 5)
            {
                logger(AREA, WARNING, "Error Counter Passed 5. Trying To Re-establish Connection.");

                establishConnection();

                if(plcConnectionErrors == 0)
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
                logger(AREA, DEBUG, "Connection Is Working.");
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
