using DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave;
using Renci.SshNet.Messages.Transport;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Twilio.Rest.Taskrouter.V1.Workspace.TaskQueue;
using Twilio.TwiML.Voice;

namespace Mirage.plc
{
    class SiemensPLC
    {
        public static libnodave.daveOSserialType fds;
        public static libnodave.daveInterface di;
        public static libnodave.daveConnection dc;
        public static bool newMsg;
        public static int res;
        public static byte plcValue;
        public static int memoryres;
        public static byte[] memoryBuffer = new byte[16];
        public static int plcConnectionErrors = 0;
        public static int serialNumber = 0;
        public static int task = 0;

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
        // Should obtain two integers:
        // - the first designates the mission type
        // - the second is a serial number
        //
        public static void poll()
        {
            int serial, task;

            // Get the data from the PLC
            memoryres = dc.readBytes(libnodave.daveFlags, 0, 0, 1, memoryBuffer);

            if(memoryres == 0)
            {
                byte[] serialInBytes = new byte[4] { memoryBuffer[0], memoryBuffer[1], memoryBuffer[2], memoryBuffer[3] };

                // Convert memory buffer from bytes to ints
                // 32 Bit int is 4 bytes
                serial = BitConverter.ToInt32(serialInBytes, 0);

                // Only change memory if the PLC is making a new request
                if(serial != serialNumber)
                {
                    // Populate Task Number

                    // Which MiR to affect?

                    // Other stuff

                    newMsg = true;
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
