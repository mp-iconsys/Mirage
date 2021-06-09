using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using Mirage.rest;
using static Globals;
using static Globals.DebugLevel;
using System.Data;
using System.Threading;
using System.Diagnostics;

namespace Mirage.mir
{
    public class WiFi
    {
        //=========================================================|
        // WiFi Connection Details                                 |
        //=========================================================|
        public string uuid { get; set; }
        public string bssid { get; set; }
        public string url { get; set; }
        public string mac { get; set; }
        public string name { get; set; }
        bool connected { get; set; }

        //=========================================================|
        // WiFi Network Scan                                       |
        // This requires a GUID                                    |
        //=========================================================|
        public int strength { get; set; }
        public int channel { get; set; }
        public string ssid { get; set; }
        public string frequency { get; set; }
        public string guid { get; set; }

        //=========================================================|
        // REST Data Containers                                    |
        //=========================================================|
        public Network wifiNetworkDetails;

        //===============================================================================|
        // First, you need to fetch the network guid using GET /wifi/networks            |
        // Then, you can fetch the strength using GET /wifi/networks/{guid}              |
        // This data needs to be married together with the X and Y positions             |
        // And then recorded in a database and mapped                                    |
        //===============================================================================|
        public WiFi()
        {
            uuid = "";
            bssid = "";
            url = "";
            mac = "";
            name = "";
            connected = false;

            strength = 0;
            channel = 0;
            ssid = "";
            frequency = "";
            guid = "";
        }


    }
}
