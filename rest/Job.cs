using System;
using System.Collections.Generic;
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

        public long job { get; set; }
        public int currentMission { get; set; }
        public int totalNoOfMissions { get; set; }
        public DateTime start { get; set; }
        public DateTime end { get; set; }
        public List<JobMission> missions{ get; set; }

        public Job()
        {
            job = 0;
            currentMission = 0;
            missions = new List<JobMission>();
        }

        public void startJob()
        {
            job++;
            start = DateTime.Now;
            missions = new List<JobMission>();
        }

        public void startJob(int missionID, string missionName)
        {
            startJob();
            missions.Add(new JobMission(missionID, missionName));
            logger(AREA, INFO, "Starting Job " + job);
        }

        public void addMission(int missionID, string missionName)
        {
            missions[currentMission].end_time = DateTime.Now;
            missions[currentMission].print();

            currentMission++;

            missions.Add(new JobMission(missionID, missionName));
        }

        public void finishJob()
        {
            end = DateTime.Now;
            missions[currentMission].end_time = DateTime.Now;
            totalNoOfMissions = currentMission;

            // Save Job Data to DB

            // Save all the missions to DB

            // Release the missions and clear the Job data
            missions = new List<JobMission>();
            start = DateTime.Now;
            currentMission = 0;
        }

        public void saveJob()
        {

        }

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
        }
    }
}
