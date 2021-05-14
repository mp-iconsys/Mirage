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
        public string uuid;
        public string bssid;
        public string url;
        public string mac;
        public string name;
        bool connected;

        //=========================================================|
        // WiFi Network Scan                                       |
        // This required a GUID                                    |
        //=========================================================|
        public int strength;
        public int channel;
        public string ssid;
        public string frequency;
        public string guid;

        //=========================================================|
        // REST Data Containers                                    |
        //=========================================================|
        public Network wifiNetworkDetails;

    }
}
