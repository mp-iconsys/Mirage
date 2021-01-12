using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using static Globals;
using static Globals.DebugLevel;

// Fore generating Excel
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;

namespace Mirage.Reporting
{
    public class Reporting
    {
        public int report_id;
        public DateTime start_date;
        public DateTime end_date;

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Reporting);

        public Reporting() { }

        public bool checkReportTriggers()
        {
            // Check if we have to generate a report by scanning the database
            try
            {
                bool activateReports = false;

                string sql = "SELECT * FROM report_settings WHERE PROCESS = 0;";
                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    logger(AREA, DEBUG, "ID: " + rdr.GetInt32(0) + " - Date From: " + rdr.GetString(2) + " - Date To: " + rdr.GetString(3));

                    report_id = rdr.GetInt32(0);
                    start_date = rdr.GetDateTime(2);
                    end_date = rdr.GetDateTime(3);

                    activateReports = true;
                }

                return activateReports;
            }
            catch
            {
                logger(AREA, ERROR, "Failed To Check DB For Reporting");
                return false;
            }
        }

        public string fetchReportData(bool fetch_data)
        {
            string sql;

            if(!fetch_data)
            {
                // No command to fetch data and generate reports - Do nothing
                logger(AREA, DEBUG, "No Commands To Generate Reports");
                sql = "";
            }
            else
            {
                // No command to fetch data and generate reports - Do nothing
                logger(AREA, INFO, "Fetch Data For Report " + report_id);

                //start_date = start_date.ToString("MM-dd-yyyy HH:mm:ss");

                if (report_id == 0)
                {
                    sql = "CALL report_0('" + start_date.ToString("yyyy-MM-dd HH:mm") + "', '" + end_date.ToString("yyyy-MM-dd HH:mm") + "')";
                }
                else if (report_id == 1)
                {
                    sql = "CALL report_1('" + start_date.ToString("yyyy-MM-dd HH:mm") + "', '" + end_date.ToString("yyyy-MM-dd HH:mm") + "')";
                }
                else if (report_id == 2)
                {
                    sql = "CALL report_2('" + start_date.ToString("yyyy-MM-dd HH:mm") + "', '" + end_date.ToString("yyyy-MM-dd HH:mm") + "')";
                }
                else
                {
                    sql = "";
                }
            }

            return sql;
        }
    
        public void generateReport1(bool reportsNeeded, string sql)
        {
            if(reportsNeeded)
            { 
                string filename = "test.xlsx";

                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet1 = workbook.CreateSheet("Report 1");
                IRow row = sheet1.CreateRow(0);

                //font style1: underlined, italic, red color, fontsize=20
                IFont font1 = workbook.CreateFont();
                font1.Color = IndexedColors.Black.Index;
                font1.IsItalic = false;
                font1.IsBold = true;
                font1.FontHeightInPoints = 15;

                //bind font with style 1
                ICellStyle style1 = workbook.CreateCellStyle();
                style1.SetFont(font1);

                ICell cell = row.CreateCell(0);
                cell.SetCellValue("Mirage Data Report 1 - Overview");
                cell.CellStyle = style1;

                cell = row.CreateCell(1);
                cell.SetCellValue("Data Sampled From: ");
                cell.CellStyle = style1;

                cell = row.CreateCell(2);
                cell.SetCellValue(start_date.ToString("yyyy-MM-dd HH:mm"));
                cell.CellStyle = style1;

                cell = row.CreateCell(3);
                cell.SetCellValue("To: ");
                cell.CellStyle = style1;

                cell = row.CreateCell(4);
                cell.SetCellValue(end_date.ToString("yyyy-MM-dd HH:mm"));
                cell.CellStyle = style1;

                logger(AREA, INFO, "Generating Report 1. Start At " + DateTime.Now.ToString());

                logger(AREA, INFO, "Creating Header Row");

                sheet1.CreateRow(2).CreateCell(0).SetCellValue("Robot Data");
                row = sheet1.CreateRow(3);

                string[] headings = {"Robot ID", "Robot IP", "Model", "Name", "Uptime", "Idle Time", "Charging Time", "Mission Time", "Missions Completed", "Jobs Completed", "Average Linear Velocity", "Distance Moved", "Errors Encountered"};

                for(int i = 0; i < 13; i++)
                {
                    row.CreateCell(i).SetCellValue(headings[i]);
                }

                int x = 0;
                int spreadsheet_row = 4;

                logger(AREA, DEBUG, "Header Row Generated");
                logger(AREA, DEBUG, sql);

                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                logger(AREA, DEBUG, "Reading SQL Row");

                while (rdr.Read())
                {
                    logger(AREA, DEBUG, "Result Row: " + (spreadsheet_row - 4));

                    row = sheet1.CreateRow(spreadsheet_row);

                    for (int i = 0; i < 13; i++)
                    {
                        row.CreateCell(i).SetCellValue(rdr.GetString(i));
                    }

                    spreadsheet_row++;
                }

                logger(AREA, INFO, "End At " + DateTime.Now.ToString());
                logger(AREA, INFO, "Saving To " + filename);

                FileStream sw = File.Create(filename);
                workbook.Write(sw);
                sw.Close();

                rdr.Close();

                resetDB();
            }
            else
            {
                logger(AREA, INFO, "Reports Not Needed");
            }
        }

        public void resetDB()
        {
            using var cmd1 = new MySqlCommand("report_reset", db);

            try
            {
                cmd1.CommandType = CommandType.StoredProcedure;
                cmd1.Parameters.Add(new MySqlParameter("rep_id", report_id));

                issueQuery(cmd1);
            }
            catch (Exception exception)
            {
                cmd1.Dispose();
                logger(AREA, ERROR, "MySQL Quert Error: ", exception);
            }
        }

        public void reportingPass()
        {
            bool reportsNeeded = checkReportTriggers();
            string sql = fetchReportData(reportsNeeded);

            generateReport1(reportsNeeded, sql);
        }
    }
}
