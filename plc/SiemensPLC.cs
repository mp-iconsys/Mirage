using DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave;
using System;
using System.Net;
using Twilio.Rest.Api.V2010.Account.Usage.Record;

namespace Mirage.plc
{
    class SiemensPLC
    {
        public static libnodave.daveOSserialType fds;
        public static libnodave.daveInterface di;
        public static libnodave.daveConnection dc;

        public static int res;
        public static byte plcValue;
        public static int memoryres;
        public static byte[] memoryBuffer = new byte[16];
        public static int plcConnectionErrors = 0;


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
        public static bool newMsg;


        //
        // Remember that the PLC data blocks can't be optimized
        // in order for the program to establish connection.
        //
        // See 
        //
        public static void establishConnection()
        {
            // IP ADDRESS HERE
            fds.rfd = libnodave.openSocket(102, "10.6.1.16");
            fds.wfd = fds.rfd;
            di = new libnodave.daveInterface(fds, "IF1", 0, libnodave.daveProtoISOTCP, libnodave.daveSpeed187k);

            res = di.initAdapter();
            // For S7-1200 and 1500, use rack = 0, slot = 1
            int rack = 0;
            int slot = 1;
            dc = new libnodave.daveConnection(di, 0, rack, slot);
            res = dc.connectPLC();
            // memoryres = dc.readBytes(libnodave.daveFlags, 0, 0, 1, memoryBuffer);
            //memoryres = dc.readBytes(libnodave.daveFlags, 0, 0, 16, memoryBuffer);
            //plcValue = memoryBuffer[0];
        }

        // Polls the PLC to check if it needs to issue any new missions
        public static void poll()
        {
            int serialNumber, task, robotID, status;

            // Get the data from the PLC
            memoryres = dc.readBytes(libnodave.daveFlags, 0, 0, 1, memoryBuffer);

            if(memoryres == 0)
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
        public static void writeTo()
        {

        }

        //
        // Checks to make sure PLC processed and parsed the reponse
        //
        // INPUT:   None
        // OUTPUT:  PLC Response (success or fail)
        //
        public static int checkResponse()
        {
            int i = 0;


            return i;
        }

        public static void disconnect()
        {
            dc.disconnectPLC();
            di.disconnectAdapter();
            libnodave.closePort(fds.rfd);
        }

    }
}
