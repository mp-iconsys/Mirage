using DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave;
using System;
using System.Net;
using static Globals;

namespace Mirage.plc
{
    class SiemensPLC
    {
        public static libnodave.daveOSserialType fds;
        public static libnodave.daveInterface di;
        public static libnodave.daveConnection dc;

        // For S7-1200 and 1500, use rack = 0, slot = 1
        private static string IP = "10.6.1.16";
        private static int port = 102;
        private static int rack = 0;
        private static int slot = 1;

        //=========================================================|
        // These should reflect the Data Block Nos in PLC          |
        //                                                         |
        // We're assuming that each block is completely dedicated  |
        // to storing only this data, so they start at 0.          |    
        //=========================================================|
        private static int taskControlDB = 0;
        private static int dataStorageDB = 1;

        //=========================================================|
        //  PLC Task Control Block                                 |
        //=========================================================|

        // Start serial number on -1. The PLC value is unsigned
        // so this way we'll always pick up the first task, 
        // without risk of accidently having the same serial number in memory and in plc
        public static int serialNumber = -1; 
        public static int robotID;
        public static int task;
        public static int status;
        public static int parameter;
        public static bool newMsg;

        public static int res;
        public static byte plcValue;
        public static int memoryres;
        public static byte[] memoryBuffer = new byte[16];
        public static int plcConnectionErrors = 0;


        public static void initialize()
        {

        }


        //
        // Remember that the PLC data blocks can't be optimized
        // in order for the program to establish connection.
        //
        // See 
        //
        public static void establishConnection()
        {
            fds.rfd = libnodave.openSocket(port, IP);
            fds.wfd = fds.rfd;
            di = new libnodave.daveInterface(fds, "IF1", 0, libnodave.daveProtoISOTCP, libnodave.daveSpeed187k);

            res = di.initAdapter();
            // check if initializing the adapter worked

            dc = new libnodave.daveConnection(di, 0, rack, slot);
            res = dc.connectPLC();
            // check if connectign to the plc worked
        }

        // Polls the PLC to check if it needs to issue any new missions
        public static void poll()
        {
            int serialNumber, task, robotID, status;

            // Get the data from the PLC
            try
            { 
                // readBytes(Area, Data Block Number (in PLC), Start Byte, Length, Byte Container)
                memoryres = dc.readBytes(libnodave.daveFlags, taskControlDB, 0, 16, memoryBuffer);
            }
            catch(Exception e)
            {

            }

            if (memoryres == 0)
            {
                byte[] tempBytesForConversion = new byte[4] { memoryBuffer[0], memoryBuffer[1], memoryBuffer[2], memoryBuffer[3] };

                // Convert memory buffer from bytes to ints
                // 32 Bit int is 4 bytes
                serialNumber = BitConverter.ToInt32(tempBytesForConversion, 0);

                // Only change memory if the PLC is making a new request
                if(serialNumber != SiemensPLC.serialNumber)
                {
                    // This determines which MiR to affect
                    tempBytesForConversion = new byte[4] { memoryBuffer[4], memoryBuffer[5], memoryBuffer[6], memoryBuffer[7] };
                    robotID = BitConverter.ToInt32(tempBytesForConversion, 0);

                    // This determines which action to perform
                    tempBytesForConversion = new byte[4] { memoryBuffer[8], memoryBuffer[9], memoryBuffer[10], memoryBuffer[11] };
                    task = BitConverter.ToInt32(tempBytesForConversion, 0);

                    // This is the status of the request - should be 0 stuff
                    tempBytesForConversion = new byte[4] { memoryBuffer[12], memoryBuffer[13], memoryBuffer[14], memoryBuffer[15] };
                    status = BitConverter.ToInt32(tempBytesForConversion, 0);

                    newMsg = true;
                    SiemensPLC.status = status;
                    SiemensPLC.task = task;
                    SiemensPLC.robotID = robotID;
                    SiemensPLC.serialNumber = serialNumber;
                }
                else
                {
                    newMsg = false;
                }
            }
            else
            {
                // TODO: look at libnodave for better error codes
                // Couldn't read the PLC bytes
                // Write as error to DB
                // Iterate the PLC connectivity counter
                plcConnectionErrors++;
            }
        }

        //
        // A function to write data to a PLC at the end of an operation
        // Should write three values:
        // - if the assigned task was successful
        // - the serial number of the task
        // - data to be used for the PLC (batter life, etc)
        //
        // Defined on generic types so it can take floats, ints, strings 
        public static void writeFloatData(string type, int statusCode, float data)
        {
            // First, check if the data response was successful
            if(statusCode == Status.CompletedPartially || statusCode == Status.CompletedNoErrors)
            {
                byte[] tempBytes; 
                int result = 0;

                // TODO: This should really use an enum instead of strings
                if (type == "moved")
                {
                    tempBytes = BitConverter.GetBytes(data);
                    result = dc.writeBytes(libnodave.daveFlags, dataStorageDB, 12, 4, tempBytes); // location is incorrect atm
                }
                else if(type == "battery")
                {
                    tempBytes = BitConverter.GetBytes(data);
                    result = dc.writeBytes(libnodave.daveFlags, dataStorageDB, 12, 4, tempBytes); // location is incorrect atm
                }

                if (result != 0)
                {
                    // Flag error

                }
            }
            else if(statusCode == Status.CouldntProcessRequest)
            {
                updateTaskStatus(Status.CouldntProcessRequest);
                // Send alert
            }
            else if(statusCode == Status.FatalError)
            {
                updateTaskStatus(Status.FatalError);
                // Send alert
            }
            else
            {
                // Unknown Status ??? - > Treat like a fatal error
                updateTaskStatus(Status.FatalError);
                // Send alert
            }
        }

        public static void writeStringData(string type, int statusCode, string data)
        {
            // First, check if the data response was successful
            if (statusCode == Status.CompletedPartially || statusCode == Status.CompletedNoErrors)
            {
                byte[] tempBytes;
                int result = 0;

                if (type == "mission_text")
                {
                    tempBytes = System.Text.Encoding.ASCII.GetBytes(data);
                    result = dc.writeBytes(libnodave.daveFlags, dataStorageDB, 12, 4, tempBytes); // location is incorrect atm
                }

                if(result != 0)
                {
                    // Flag error

                }
            }
            else if (statusCode == Status.CouldntProcessRequest)
            {
                updateTaskStatus(Status.CouldntProcessRequest);
                // Send alert
            }
            else if (statusCode == Status.FatalError)
            {
                updateTaskStatus(Status.FatalError);
                // Send alert
            }
            else
            {
                // Unknown Status ??? - > Treat like a fatal error
                updateTaskStatus(Status.FatalError);
                // Send alert
            }
        }


        public static void updateTaskStatus(int status)
        {
            try
            {
                byte[] tempBytes = BitConverter.GetBytes(status);

                int result = dc.writeBytes(libnodave.daveFlags, taskControlDB, 12, 4, tempBytes);

                if(result != 0)
                {
                    // Log error here
                }
            }
            catch(Exception e)
            {

            }
        }

        //
        // Checks to make sure PLC processed and parsed the reponse
        //
        // INPUT:   None
        // OUTPUT:  Send Alert, or nothing
        //
        public static void checkResponse()
        {
            int i = 0;
            byte[] tempByteBuffer = new byte[4];

            try
            {
                // readBytes(Area, Data Block Number (in PLC), Start Byte, Length, Byte Container)
                memoryres = dc.readBytes(libnodave.daveFlags, taskControlDB, 12, 4, tempByteBuffer);
            }
            catch (Exception e)
            {

            }

            // PLC read was successful
            if (memoryres == 0)
            {
                status = BitConverter.ToInt32(tempByteBuffer, 0);

                if(status == Status.PlcOK)
                {
                    // Do nothing
                }
                else if(status == Status.PlcError)
                {
                    // Send error
                }
                else
                {

                }               
            }
            else
            {
                // Failed to get the status data from PLC
                // Send error
            }

            // Time-out if couldn't find the status
        }

        public static void disconnect()
        {
            dc.disconnectPLC();
            di.disconnectAdapter();
            libnodave.closePort(fds.rfd);
        }
    }
}
