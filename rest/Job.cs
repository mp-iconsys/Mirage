using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.rest
{
    public class Job
    {
        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Job);

        public bool isJobInProgress { get; set; }
        public long job { get; set; }
        public int currentMission { get; set; }
        public int totalNoOfMissions { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public List<JobMission> missions{ get; set; }

        public Job()
        {
            isJobInProgress = true;
            job = 0;
            currentMission = 0;
            missions = new List<JobMission>();  
        }

        public Job(int jobNo)
        {
            isJobInProgress = true;
            job = jobNo;
            currentMission = 0;
            missions = new List<JobMission>();
        }

        public void getLatestJob(int robotID)
        {
            try
            {
                string sql = "SELECT MAX(JOB_ID) FROM jobs WHERE ROBOT_ID = " + robotID + ";";
                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    job = rdr.GetInt32(0) + 1;
                }

                cmd.Dispose();
                rdr.Close();
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Get The Latest Job");
                logger(AREA, ERROR, "Exception: ", e);
            }
        }

        public void startJob()
        {
            try
            {
                isJobInProgress = true;
                job++;
                currentMission = 0;
                start = DateTime.Now;
                missions = new List<JobMission>();
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Initialize A Job");
                logger(AREA, ERROR, "Exception", e);
            }
        }

        public void startJob(int missionID, string missionName)
        {
            try
            { 
                startJob();
                missions.Add(new JobMission(missionID, missionName));
                //missions[currentMission] = new JobMission(missionID, missionName);

                logger(AREA, INFO, "Starting A New Job " + job);
                logger(AREA, INFO, "Robot's " + currentMission + " Mission");
                missions[currentMission].print();
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Initialize A Job");
                logger(AREA, ERROR, "Exception", e);
            }
        }

        public void addMission(int missionID, string missionName)
        {
            try
            { 
                if(isJobInProgress)
                { 
                    logger(AREA, INFO, "Adding A New Mission To The Stack. It Will Be Robot's " + currentMission + " Mission");

                    missions.Add(new JobMission(missionID, missionName));

                    logger(AREA, INFO, "Mission Added");

                    currentMission++;

                    //missions[currentMission] = new JobMission(missionID, missionName);

                    logger(AREA, INFO, "New Mission: " + currentMission);

                    missions[currentMission].print();
                }
                else
                {
                    logger(AREA, INFO, "There's No Jobs In Progress. We're Starting In The Middle Of A Sequence.");

                    startJob(missionID, missionName);
                }
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Add A Mission. Will Try Again After Creating A Job.");
                logger(AREA, ERROR, "Exception :", e);
                startJob(missionID, missionName);
            }
        }

        public void finishMission()
        {
            try
            { 
                missions[currentMission].end_time = DateTime.Now;

                logger(AREA, INFO, "Finished Robot's " + currentMission + " Mission");
                missions[currentMission].print();
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Finish A Mission.");
                logger(AREA, ERROR, "Exception: ", e);
            }
        }

        public void finishJob(int robotID, bool isAborted)
        {
            try
            {
                if(isJobInProgress)
                {
                    totalNoOfMissions = currentMission;

                    logger(AREA, INFO, "Finishing Job " + job + ". It Had " + totalNoOfMissions + " Missions In Total");

                    isJobInProgress = false;
                    end = DateTime.Now; 
                    missions[currentMission].end_time = DateTime.Now;
                    

                    // Save Job Data to DB
                    saveJob(robotID, isAborted);

                    // Save all the missions to DB
                    saveMissions(robotID);
                }
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Finalize Job Details");
                logger(AREA, ERROR, "Exception: ", e);
            }

            // Release the missions and clear the Job data
            try
            {
                missions = new List<JobMission>();
                start = DateTime.Now;
                currentMission = 0;
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Clear Job");
                logger(AREA, ERROR, "Exception: ", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void saveJob(int robotID, bool isAborted)
        {
            logger(AREA, INFO, "Saving The Job In The DB");

            try
            {
                MySqlCommand cmd = new MySqlCommand();
                cmd.Connection = db;
                cmd.CommandText = "store_job";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@JOB_ID", job);
                cmd.Parameters["@JOB_ID"].Direction = ParameterDirection.Input;

                cmd.Parameters.AddWithValue("@ROBOT_ID", robotID);
                cmd.Parameters["@ROBOT_ID"].Direction = ParameterDirection.Input;

                cmd.Parameters.AddWithValue("@NO_OF_MISSIONS", totalNoOfMissions);
                cmd.Parameters["@NO_OF_MISSIONS"].Direction = ParameterDirection.Input;

                if(isAborted)
                {
                    cmd.Parameters.AddWithValue("@ABORTED", 1);
                    cmd.Parameters["@ABORTED"].Direction = ParameterDirection.Input;
                }
                else
                {
                    cmd.Parameters.AddWithValue("@ABORTED", 0);
                    cmd.Parameters["@ABORTED"].Direction = ParameterDirection.Input;
                }

                cmd.Parameters.AddWithValue("@START_TIME", start.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters["@START_TIME"].Direction = ParameterDirection.Input;

                cmd.Parameters.AddWithValue("@END_TIME", end.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters["@END_TIME"].Direction = ParameterDirection.Input;

                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
            catch (Exception exception)
            {
                logger(AREA, ERROR, "MySQL Query Error: ", exception);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void saveMissions(int robotID)
        {
            logger(AREA, INFO, "Saving Missions In The DB");
            logger(AREA, INFO, "Total No Of Missions: " + totalNoOfMissions);

            for (int i = 0; i < (totalNoOfMissions+1); i++)
            {
                logger(AREA, INFO, "Going Through Robot's Mission: " + i);
                missions[i].print();

                try 
                { 
                    MySqlCommand cmd = new MySqlCommand();
                    cmd.Connection = db;
                    cmd.CommandText = "store_job_mission";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@JOB_ID", job);
                    cmd.Parameters["@JOB_ID"].Direction = ParameterDirection.Input;

                    cmd.Parameters.AddWithValue("@ROBOT_ID", robotID);
                    cmd.Parameters["@ROBOT_ID"].Direction = ParameterDirection.Input;

                    cmd.Parameters.AddWithValue("@MISSION_ID", missions[i].mission);
                    cmd.Parameters["@MISSION_ID"].Direction = ParameterDirection.Input;

                    cmd.Parameters.AddWithValue("@MISSION_NAME", missions[i].mission_name);
                    cmd.Parameters["@MISSION_NAME"].Direction = ParameterDirection.Input;

                    cmd.Parameters.AddWithValue("@START_TIME", missions[i].start_time.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters["@START_TIME"].Direction = ParameterDirection.Input;

                    cmd.Parameters.AddWithValue("@END_TIME", missions[i].end_time.ToString("yyyy-MM-dd HH:mm:ss"));
                    cmd.Parameters["@END_TIME"].Direction = ParameterDirection.Input;

                    cmd.ExecuteNonQuery();
                    cmd.Dispose();
                }
                catch (Exception e)
                {
                    logger(AREA, ERROR, "MySQL Query Error: ", e);
                }
            }
        }

        /// <summary>
        /// A class that contains relevant data for 
        /// </summary>
        public class JobMission
        {
            public int mission { get; set; }
            public string mission_name { get; set; }
            public DateTime start_time { get; set; }
            public DateTime end_time { get; set; }

            public JobMission()
            {
                mission = 0;
                mission_name = "Empty";
                start_time = DateTime.Now;
                end_time = DateTime.Now;
            }

            public JobMission(int id, string name)
            {
                mission = id;
                mission_name = name;
                start_time = DateTime.Now;
            }

            public void print()
            {
                logger(AREA, INFO, "Mission: " + mission + " " + mission_name + " Started: " + start_time + " Finished: " + end_time);
            }

            public void saveMissionInDB()
            {

            }
        }
    }
}
