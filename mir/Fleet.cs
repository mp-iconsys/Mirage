using Mirage.rest;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage
{
    public class Fleet
    {
        //=========================================================|
        // Fleet manager is essentially a robot                    |
        // keep it separate from the robot array for cleaner code  |
        //=========================================================|
        // TODO : Make these all lists, so the size can be amended at runtime
        public Robot[] robots;
        public Robot fleetManager;
        private Task<HttpResponseMessage>[] httpResponseTasks;
        private Task<HttpResponseMessage> fleetResponseTask;
        public RobotGroup[] group;

        public ChargingGroup busy;
        public ChargingGroup available;

        public short returnParameter = 0;
        public short[] groups = new short[8] { (short)sizeOfFleet, 0, 0, 0, 0, 0, 0, 0 };

        public int available_group = 1;
        public int busy_group = 2;
        public int offline_group = 3;

        public int fleet_available_group = 3;
        public int fleet_busy_group = 4;
        public int fleet_offline_group = 5;

        public bool scanRegisters = false;

        //public int[] robotMapping;

        //=========================================================|
        //  Helper parameters                                      |
        //=========================================================|
        int waitPeriod = 100; // Used when getting initial fleet data so we don't DDOS our own robots

        //=========================================================|
        //  Used For Debugging                                     |     
        //=========================================================|
        private static readonly Type AREA = typeof(Fleet);

        /// <summary>
        /// Initializes the robot fleet, Fleet Manager excluded
        /// </summary>
        /// /// <param name="sizeOfFleet">Number of robots in the fleet</param>
        public Fleet(int sizeOfFleet)
        {
            robots = new Robot[sizeOfFleet];
           // robotMapping = new int[2] { 4, 2 };
            httpResponseTasks = new Task<HttpResponseMessage>[sizeOfFleet];

            // Instantiates the group array
            groups = new short[8] { 0, (short)sizeOfFleet, 0, 0, 0, 0, 0, 0 };

            // Initialize the robot group
            group = new RobotGroup[8];
            for(int i = 0; i < 8; i++)
            {
                group[i] = new RobotGroup(); 
            }

            available = new ChargingGroup(2, "FullCharge", "/v2.0.0/charging_groups/2");
            busy = new ChargingGroup(3, "EmptyCharge", "/v2.0.0/charging_groups/3");

            instantiateRobots(sizeOfFleet);
        }

        /// <summary>
        /// Initializes the robot fleet including the Fleet Manager
        /// </summary>
        /// <param name="sizeOfFleet">Number of robots in the fleet</param>
        /// <param name="fleetManagerIP">The IP of the Fleet Manager</param>
        /// <param name="fleetManagerAuthToken">Authentication Token of the Fleet Manager</param>
        public Fleet(int sizeOfFleet, string fleetManagerIP, AuthenticationHeaderValue fleetManagerAuthToken)
        {
            robots = new Robot[sizeOfFleet];
            //robotMapping = new int[2] { 4, 2 };
            httpResponseTasks = new Task<HttpResponseMessage>[sizeOfFleet];

            // Instantiates the group array
            groups = new short[8] { 0, (short)sizeOfFleet, 0, 0, 0, 0, 0, 0 };
            logger(AREA, DEBUG, "Assigned Basics");

            available = new ChargingGroup(2, "FullCharge", "/v2.0.0/charging_groups/2");
            busy = new ChargingGroup(3, "EmptyCharge", "/v2.0.0/charging_groups/3");

            instantiateRobots(sizeOfFleet, fleetManagerIP, fleetManagerAuthToken);
        }

        /// <summary>
        /// Instantiates the mir fleet without the fleet manager.
        /// </summary>
        /// <param name="sizeOfFleet"></param>
        public void instantiateRobots(int sizeOfFleet)
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                // Instantiate the robots - Don't touch the tasks yet
                robots[i] = new Robot(i);
            }
        }

        /// <summary>
        /// Instantiates the mir fleet including the fleet manager.
        /// </summary>
        /// <param name="sizeOfFleet">Number of robots in the fleet</param>
        /// <param name="fleetManagerIP">The IP of the Fleet Manager</param>
        /// <param name="fleetManagerAuthToken">Authentication Token of the Fleet Manager</param>
        public void instantiateRobots(int sizeOfFleet, string fleetManagerIP, AuthenticationHeaderValue fleetManagerAuthToken)
        {
            fleetManager = new Robot(fleetManagerIP, fleetManagerAuthToken);
            fleetManager.id = fleetID;

            logger(AREA, DEBUG, "Completed Fleet Assignment");

            for (int i = 0; i < sizeOfFleet; i++)
            {
                robots[i] = new Robot(i);
            }

            logger(AREA, DEBUG, "Completed Robot Assignment");
        }

        /// <summary>
        /// Issues HTTP Get Requests to all MiR robots in memory.
        /// </summary>
        /// <param name="type">Defines the request type</param>
        public void issueGetRequests(string type)
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if(robots[i].isLive)
                { 
                    try
                    {
                        try
                        {
                            httpResponseTasks[i] = robots[i].sendGetRequest(type);
                        }
                        catch (HttpRequestException exception)
                        {
                            // TODO: Remove the task which is causing the exception
                            logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                            logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                            robots[i].deadRobotAlarm();
                        }
                    }
                    catch (System.Net.WebException exception)
                    {
                        logger(AREA, ERROR, "HTTP WebException Connection Error In Fleet.isueGetRequests: ", exception);
                        robots[i].deadRobotAlarm();
                    }
                }
                else if(robots[i].deadRobotAlarmNotTriggered)
                {
                    logger(AREA, WARNING, "Robot " + i + " Is Not Live");
                }
            }
        }

        /// <summary>
        /// Sync method that issues a get request and saves data in memory. Used to issue tasks as a middleman for the PLC.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="robotID"></param>
        /// <returns></returns>
        public int issueGetRequest(string type, int robotID)
        {
            logger(AREA, DEBUG, "==== Issuing Get Request ====");

            int functionStatus = Globals.TaskStatus.CompletedNoErrors;

            bool robotIsAlive;

            if(robotID == fleetID)
            {
                robotIsAlive = fleetManager.isLive;
            }
            else
            {
                robotIsAlive = robots[robotID].isLive;
            }

            if (robotIsAlive)
            {
                try
                {
                    try
                    {
                        if (type == "mission_scheduler" && robotID == 666)
                        {
                            logger(AREA, DEBUG, "Sending " + type + " Request To Fleet Manager");
                            fleetResponseTask = fleetManager.sendGetRequest(type);
                        }
                        else if (type == "robots" && robotID == 666)
                        {
                            logger(AREA, DEBUG, "Sending " + type + " To Fleet Manager");
                            fleetResponseTask = fleetManager.sendGetRequest(type);
                        }
                        else if (type == "robotStatusFromFleet" && robotID != 666)
                        {
                            logger(AREA, DEBUG, "Sending " + type + " To Fleet Manager");

                            fleetResponseTask = fleetManager.sendGetRequest("robots/" + mirFleet.robots[robotID].fleetRobotID);
                        }
                        else if (type == "missions" && robotID == 666)
                        {
                            logger(AREA, DEBUG, "Sending " + type + " Request To Fleet Manager");
                            fleetResponseTask = fleetManager.sendGetRequest(type);
                        }
                        else if (robotID == 666)
                        {
                            fleetResponseTask = fleetManager.sendGetRequest(type);
                        }
                        else
                        {
                            logger(AREA, DEBUG, "Sending " + type + " To Robot No " + robotID);
                            httpResponseTasks[robotID] = robots[robotID].sendGetRequest(type);
                        }
                    }
                    catch (HttpRequestException exception)
                    {
                        // TODO: Handle more exceptions
                        // Remove the task which is causing the exception
                        logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                        logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);

                        if(robotID == fleetID)
                        { 
                            fleetManager.deadRobotAlarm();
                        }
                        else
                        {
                            robots[robotID].deadRobotAlarm();
                        }

                        functionStatus = Globals.TaskStatus.CouldntProcessRequest;
                    }
                }
                catch (System.Net.WebException exception)
                {
                    logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);

                    if (robotID == fleetID)
                    {
                        fleetManager.deadRobotAlarm();
                    }
                    else
                    {
                        robots[robotID].deadRobotAlarm();
                    }

                    functionStatus = Globals.TaskStatus.CouldntProcessRequest;
                }

                try
                {
                    if (type == "mission_scheduler" || robotID == 666)
                    {
                        fleetResponseTask.Wait();
                    }
                    else if (robotID == 666)
                    {
                        fleetResponseTask.Wait();
                    }
                    else if (type == "mission_scheduler/666")
                    {
                        fleetResponseTask.Wait();
                    }
                    else
                    {
                        httpResponseTasks[robotID].Wait();

                        logger(AREA, DEBUG, "Waiting For HTTP Response Task");
                    }
                }
                catch (Exception exception)
                {
                    functionStatus = Globals.TaskStatus.CouldntProcessRequest;

                    if (robotID == fleetID)
                    {
                        fleetManager.deadRobotAlarm();
                    }
                    else
                    {
                        robots[robotID].deadRobotAlarm();
                    }

                    logger(AREA, ERROR, "Connection Error: ", exception);
                }

                if (type == "status")
                {
                    robots[robotID].saveStatusInMemory(httpResponseTasks[robotID].Result);

                    logger(AREA, DEBUG, "Status is : " + robots[robotID].s.mission_text);
                }
                else if (type == "robotStatusFromFleet")
                {
                    Robots g;
                    g = new Robots();
                    g = JsonConvert.DeserializeObject<Robots>(fleetResponseTask.Result.Content.ReadAsStringAsync().Result);

                    robots[robotID].s.robot_group_id = g.robot_group_id - 2;
                    robots[robotID].s.mode_id = g.status.mode_id;
                    robots[robotID].s.state_id = g.status.state_id;
                    robots[robotID].s.uptime = g.status.uptime;
                    robots[robotID].s.battery_time_remaining = g.status.battery_time_remaining;
                    robots[robotID].s.battery_percentage = g.status.battery_percentage;
                    robots[robotID].s.distance_to_next_target = g.status.distance_to_next_target;
                    robots[robotID].s.moved = (float)g.status.moved;
                    robots[robotID].s.footprint = g.status.footprint;
                    robots[robotID].s.joystick_low_speed_mode_enabled = g.status.joystick_low_speed_mode_enabled;
                    robots[robotID].s.joystick_web_session_id = g.status.joystick_web_session_id;
                    robots[robotID].s.map_id = g.status.map_id;
                    robots[robotID].s.mission_queue_id = g.status.mission_queue_id.ToString();
                    robots[robotID].s.mission_text = g.status.mission_text;
                    robots[robotID].s.mode_key_state = g.status.mode_key_state;
                    robots[robotID].s.mode_text = g.status.mode_text;
                    robots[robotID].s.robot_name = g.status.robot_name;
                    robots[robotID].s.safety_system_muted = g.status.safety_system_muted;
                    robots[robotID].s.session_id = g.status.session_id;
                    robots[robotID].s.state_text = g.status.state_text;
                    robots[robotID].s.unloaded_map_changes = g.status.unloaded_map_changes;

                    robots[robotID].s.position.x = g.status.position.x;
                    robots[robotID].s.position.y = g.status.position.y;
                    robots[robotID].s.position.orientation = g.status.position.orientation;

                    robots[robotID].s.velocity.linear = g.status.velocity.linear;
                    robots[robotID].s.velocity.angular = g.status.velocity.angular;
                    robots[robotID].s.position.x = g.status.position.x;

                    if(g.status.errors.Count > 0)
                    { 
                        robots[robotID].s.code = g.status.errors[0].code;
                        robots[robotID].s.description = g.status.errors[0].description;
                        robots[robotID].s.module = g.status.errors[0].module;
                    }
                    else
                    {
                        robots[robotID].s.code = 0;
                        robots[robotID].s.description = "0";
                        robots[robotID].s.module = "0";
                    }

                    //int a = 
                    //= new rest.Robots();
                    //g = JsonConvert.DeserializeObject<rest.Robots>(fleetResponseTask.Result.Content.ReadAsStringAsync().Result);
                    //_ = g.Root.fleet_state;

                    // Robot Group is offset by 2 from the fleet robot group
                    //robots[robotID].s.robot_group_id = g.robot_group_id - 2;

                    //robots[robotID].saveStatusInMemory(fleetResponseTask.Result);

                    logger(AREA, DEBUG, "Got Status From Fleet");
                }
                else if (type == "mission_scheduler/" + mirFleet.fleetManager.schedule.id)
                {
                    logger(AREA, DEBUG, "Getting Mission Scheduler response");

                    fleetManager.schedule.saveToMemory(fleetResponseTask.Result);
                }
                else if (type == "missions")
                {
                    logger(AREA, DEBUG, "Hellow from missions save");
                    fleetManager.saveMissions(fleetResponseTask.Result);
                }
                else if (type == "robots?whitelist=robot_group_id")
                {
                    logger(AREA, DEBUG, "We're fetching robot groups in bulk");
                    group = JsonConvert.DeserializeObject<RobotGroup[]>(fleetResponseTask.Result.Content.ReadAsStringAsync().Result);

                    groups = new short[8] { 0, 0, 0, 0, 0, 0, 0, 0 };

                    //GROUPS:
                    //  0 - Spare
                    //  1 - Available
                    //  2 - Busy
                    //  3 - Charging -> No Longer Used

                    // Go through robots
                    for (int i = 0; i < sizeOfFleet; i++)
                    {
                        if (group[i].robot_group_id == 3)
                        {
                            groups[1]++;
                            group[i].robot_group_id = group[i].robot_group_id - 2;
                        }
                        else if (group[i].robot_group_id == 4)
                        {
                            groups[2]++;
                            group[i].robot_group_id = group[i].robot_group_id - 2;
                        }
                        else if (group[i].robot_group_id == 5)
                        {
                            groups[3]++;
                            group[i].robot_group_id = group[i].robot_group_id - 2;
                        }
                    }
                }

                logger(AREA, DEBUG, "==== Completed Get Request ====");
            }
            else if(robots[robotID].deadRobotAlarmNotTriggered)
            {
                logger(AREA, WARNING, "Robot " + robotID + " Is Not Live");
            }

            return functionStatus;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="robotID"></param>
        /// <param name="robot"></param>
        /// <returns></returns>
        public int checkMissionSchedule(string type, int robotID, int robot)
        {
            // TODO: joing with fleet

            logger(AREA, DEBUG, "==== Checking Mission Scheduler ====");

            int functionStatus = Globals.TaskStatus.CompletedNoErrors;

            bool robotIsAlive;

            if (robotID == fleetID)
            {
                robotIsAlive = fleetManager.isLive;
            }
            else
            {
                robotIsAlive = robots[robotID].isLive;
            }

            if (robotIsAlive)
            {
                try
                {
                    try
                    {
                        if (type == "mission_scheduler" || robotID == 666)
                        {
                            logger(AREA, DEBUG, "Sending " + type + " Request To Fleet Manager");
                            fleetResponseTask = fleetManager.sendGetRequest(type);
                        }
                    }
                    catch (HttpRequestException exception)
                    {
                        // TODO: Handle more exceptions
                        // Remove the task which is causing the exception
                        logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                        logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);


                        if (robotID == fleetID)
                        {
                            fleetManager.deadRobotAlarm();
                        }
                        else
                        {
                            robots[robotID].deadRobotAlarm();
                        }

                        functionStatus = Globals.TaskStatus.CouldntProcessRequest;
                    }
                }
                catch (System.Net.WebException exception)
                {
                    logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);


                    if (robotID == fleetID)
                    {
                        fleetManager.deadRobotAlarm();
                    }
                    else
                    {
                        robots[robotID].deadRobotAlarm();
                    }

                    functionStatus = Globals.TaskStatus.CouldntProcessRequest;
                }

                try
                {
                    if (type == "mission_scheduler" || robotID == 666)
                    {
                        fleetResponseTask.Wait();
                    }
                    else if (robotID == 666)
                    {
                        fleetResponseTask.Wait();
                    }
                    else
                    {
                        httpResponseTasks[robotID].Wait();

                        logger(AREA, DEBUG, "Waiting For HTTP Response Task");
                    }
                }
                catch (Exception exception)
                {
                    functionStatus = Globals.TaskStatus.CouldntProcessRequest;

                    if (robotID == fleetID)
                    {
                        fleetManager.deadRobotAlarm();
                    }
                    else
                    {
                        robots[robotID].deadRobotAlarm();
                    }

                    logger(AREA, ERROR, "Connection Error: ", exception);
                }

                if (type == "status")
                {
                    robots[robotID].saveStatusInMemory(httpResponseTasks[robotID].Result);

                    logger(AREA, DEBUG, "Status is : " + robots[robotID].s.mission_text);
                }
                else
                {

                    logger(AREA, DEBUG, "Getting Mission Scheduler response");

                    mirFleet.robots[robot].schedule.saveToMemory(fleetResponseTask.Result);
                    //fleetManager.schedule.saveToMemory(fleetResponseTask.Result);  
                }
            }
            else if(robots[robotID].deadRobotAlarmNotTriggered)
            {
                logger(AREA, WARNING, "Robot " + robotID + " Is Not Live");
            }

            return functionStatus;
        }

        /// <summary>
        /// Get Robot Ids from the Fleet. These are different from our internal IDs as well as the ones used in the PLC
        /// </summary>
        public void getFleetRobotIDs()
        {
            //=========================================================|
            // Get Fleet Robot IDs                                     |
            //=========================================================|
            try
            {
                logger(AREA, INFO, "Getting Fleet Robot IDs");

                fleetResponseTask = fleetManager.sendGetRequest("robots?whitelist=ip,id");

                fleetResponseTask.Wait();

                group = JsonConvert.DeserializeObject<RobotGroup[]>(fleetResponseTask.Result.Content.ReadAsStringAsync().Result);

                // Go through each Mirage robot
                for(int i = 0; i < sizeOfFleet; i++)
                {
                    logger(AREA, DEBUG, "Going Through Robot: " + i);
                    logger(AREA, DEBUG, "Robot IP is: " + robots[i].ipAddress);

                    bool foundMatch = false;

                    // Go through each return parameter
                    for (int j = 0; j < group.Length; j++)
                    {
                        logger(AREA, DEBUG, "Checking Fleet Robot: " + group[j].id + " With IP: " + group[j].ip);

                        // For each robot and return parameter, compare their IP address
                        // If they match, assign a Mirage robot the corresponding fleet robot ID
                        if (robots[i].ipAddress == group[j].ip)
                        {
                            foundMatch = true;

                            robots[i].fleetRobotID = group[j].id;
                            logger(AREA, INFO, "Mirage Robot: " + robots[i].id + " Corresponds To Fleet Robot: " + group[j].id);

                            break;
                        }
                    }

                    if(!foundMatch)
                    {
                        logger(AREA, ERROR, "Robot " + robots[i].id + " Hasn't Found A Match In Fleet");
                    }
                }
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Error getting robot IDs from Fleet: ", e);
            }
        }

        /// <summary>
        /// Gets an internal (mirage) robot id from a given fleet robot ID
        /// </summary>
        /// <param name="fleetID"></param>
        /// <returns></returns>
        public int getInternalRobotID(int fleetID)
        {
            // Error Code
            int internalID = 999;

            for(int i = 0; i < sizeOfFleet; i++)
            {
                if(mirFleet.robots[i].fleetRobotID == fleetID)
                {
                    internalID = mirFleet.robots[i].id;
                    break;
                }
            }

            return internalID;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveFleetStatusAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if(robots[i].isLive)
                { 
                    robots[i].saveStatus(await httpResponseTasks[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveFleetRegistersAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if (robots[i].isLive)
                {
                    robots[i].saveRegisters(await httpResponseTasks[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveSoftwareLogsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if (robots[i].isLive)
                {
                    robots[i].saveSoftwareLogs(await httpResponseTasks[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveSettingsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if (robots[i].isLive)
                {
                    robots[i].saveSettings(await httpResponseTasks[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveMapsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if (robots[i].isLive)
                {
                    robots[i].saveMaps(await httpResponseTasks[i]);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task saveMissionsAsync()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if (robots[i].isLive)
                {
                    robots[i].saveMissions(await httpResponseTasks[i]);
                }
            }
        }

        public async Task saveMissionsFleet()
        {
            for (int i = 0; i < sizeOfFleet; i++)
            {
                if (robots[i].isLive)
                {
                    fleetManager.saveMissions(await fleetResponseTask);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public async void pollRobots()
        {
            logger(AREA, DEBUG, "Polling Robots");

            try
            {
                //mirFleet.issueGetRequests("status");
                //await mirFleet.saveFleetStatusAsync();
                for (int robot = 0; robot < sizeOfFleet; robot++)
                {
                    try
                    {
                        if (robots[robot].isLive)
                        {
                            mirFleet.robots[robot].s.saveToDB(robot);
                        }
                    }
                    catch (HttpRequestException exception)
                    {
                        // TODO: Handle more exceptions
                        // TODO: Remove the task which is causing the exception
                        logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                        logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                        robots[robot].deadRobotAlarm();
                    }
                }

                //mirFleet.issueGetRequests("registers");
                //await mirFleet.saveFleetRegistersAsync();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }
        }

        public async void getRegisters()
        {
            for (int robotID = 0; robotID < sizeOfFleet; robotID++)
            {
                if (mirFleet.robots[robotID].schedule.mission_number == 50 || mirFleet.robots[robotID].schedule.mission_number == 51)
                {
                    //logger(AREA, INFO, "Scanning the Registers For (doing tote in/out) " + mirFleet.robots[robotID].s.robot_name);

                    if (robots[robotID].isLive)
                    {
                        try
                        {
                            try
                            {
                                httpResponseTasks[robotID] = robots[robotID].sendGetRequest("registers");

                                if(robots[robotID].isLive)
                                { 
                                    robots[robotID].saveRegistersWithoutDB(httpResponseTasks[robotID].Result);
                                }
                            }
                            catch (HttpRequestException exception)
                            {
                                // TODO: Remove the task which is causing the exception
                                logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                                logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                                robots[robotID].deadRobotAlarm();
                            }
                        }
                        catch (System.Net.WebException exception)
                        {
                            logger(AREA, ERROR, "HTTP WebException Connection Error In Fleet.isueGetRequests: ", exception);
                            robots[robotID].deadRobotAlarm();
                        }
                    }
                    else if (robots[robotID].deadRobotAlarmNotTriggered)
                    {
                        logger(AREA, WARNING, "Robot " + robotID + " Is Not Live");
                    }
                }
                else
                {
                    scanRegisters = false;
                }
            }
        }

        /// <summary>
        /// Force the wait so we're doing things in order. Synchronous initial fetch, so there'll be some delay on start-up
        /// </summary>
        public void getInitialFleetData()
        {
            for(int i = 0; i < sizeOfFleet; i++)
            {
                logger(AREA, DEBUG, "Robot " + i + " has live status of " + robots[i].isLive.ToString());
            }

            try
            {
                try
                {
                    mirFleet.issueGetRequests("/software/logs");
                    mirFleet.saveSoftwareLogsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            logger(AREA, INFO, "Obtained Software Logs");
            Thread.Sleep(waitPeriod);

            for (int i = 0; i < sizeOfFleet; i++)
            {
                logger(AREA, DEBUG, "Robot " + i + " has live status of " + robots[i].isLive.ToString());
            }

            /*            try
                        {
                            try
                            {
                                mirFleet.issueGetRequests("maps");
                                mirFleet.saveMapsAsync().Wait();
                            }
                            catch (HttpRequestException exception)
                            {
                                // TODO: Handle more exceptions
                                // TODO: Remove the task which is causing the exception
                                logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                                logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                            }
                        }
                        catch (Exception exception)
                        {
                            logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
                        }
            */
            //logger(AREA, INFO, "Obtained Maps");
            //Thread.Sleep(waitPeriod);

            try
            {
                try
                {
                    mirFleet.issueGetRequests("settings");
                    mirFleet.saveSettingsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            for (int i = 0; i < sizeOfFleet; i++)
            {
                logger(AREA, DEBUG, "Robot " + i + " has live status of " + robots[i].isLive.ToString());
            }

            logger(AREA, INFO, "Obtained Settings");
            Thread.Sleep(waitPeriod);

            try
            {
                try
                {
                    mirFleet.issueGetRequests("settings/advanced");
                    mirFleet.saveSettingsAsync().Wait();
                }
                catch (HttpRequestException exception)
                {
                    // TODO: Handle more exceptions
                    // TODO: Remove the task which is causing the exception
                    logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                    logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                }
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
            }

            for (int i = 0; i < sizeOfFleet; i++)
            {
                logger(AREA, DEBUG, "Robot " + i + " has live status of " + robots[i].isLive.ToString());
            }

            logger(AREA, INFO, "Obtained Advanced Settings");
            Thread.Sleep(waitPeriod);

            // TODO: might need to be removed since we're creating them from scratch for the most part
            if(false)
            { 
                try
                {
                    try
                    {
                        mirFleet.issueGetRequests("missions");
                        mirFleet.saveMissionsAsync().Wait();
                    }
                    catch (HttpRequestException exception)
                    {
                        // TODO: Handle more exceptions
                        // TODO: Remove the task which is causing the exception
                        logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                        logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                    }
                }
                catch (Exception exception)
                {
                    logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
                }

                for (int i = 0; i < sizeOfFleet; i++)
                {
                    logger(AREA, DEBUG, "Robot " + i + " has live status of " + robots[i].isLive.ToString());
                }

                Thread.Sleep(waitPeriod);

                // Get Missions for the fleet
                try
                {
                    try
                    {
                        issueGetRequest("missions", fleetID);
                        mirFleet.saveMissionsFleet().Wait();
                        //mirFleet.fleetManager.saveMissionsAsync().Wait();
                    }
                    catch (HttpRequestException exception)
                    {
                        // TODO: Handle more exceptions
                        // TODO: Remove the task which is causing the exception
                        logger(AREA, ERROR, "HTTP Request Error. Couln't connect to the MiR robots.");
                        logger(AREA, ERROR, "Check your network, dns settings, robot is up, etc. Error: ", exception);
                    }
                }
                catch (Exception exception)
                {
                    logger(AREA, ERROR, "HTTP WebException Connection Error: ", exception);
                }
            
            }

            for (int i = 0; i < sizeOfFleet; i++)
            {
                logger(AREA, DEBUG, "Robot " + i + " has live status of " + robots[i].isLive.ToString());
            }

            logger(AREA, INFO, "Getting Initial Registers");

            mirFleet.getRegisters();

            // Get Initial Robot Statuses
            for (int robotID = 0; robotID < sizeOfFleet; robotID++)
            {
                if(robots[robotID].isLive)
                { 
                    issueGetRequest("status", robotID);
                }
            }

            // Get latest job numbers
            for(int robotID = 0; robotID < sizeOfFleet; robotID++)
            {
                mirFleet.robots[robotID].currentJob.getLatestJob(robotID);
            }


        }
    }
}