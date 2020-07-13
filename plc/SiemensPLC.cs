using DotNetSiemensPLCToolBoxLibrary.Communication.LibNoDave;
using Renci.SshNet.Messages.Transport;
using System;
using System.Collections.Generic;
using System.Text;

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
        public static byte[] memoryBuffer = new byte[10];

        //
        // Remember that the PLC data blocks can't be optimized
        // in order for the program to establish connection.
        //
        // See 
        //
        public static void establishConnection()
        {
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
            memoryres = dc.readBytes(libnodave.daveFlags, 0, 0, 1, memoryBuffer);
            plcValue = memoryBuffer[0];
            dc.disconnectPLC();
            di.disconnectAdapter();
            libnodave.closePort(fds.rfd);
        }

        // Polls the PLC to check if it needs to issue any new missions
        // Should obtain two integers:
        // - the first designates the mission type
        // - the second is a serial number
        //
        public static int poll()
        {
            int i = 0;


            return i;
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

    }
}
