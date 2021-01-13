using System;
using System.IO;
using System.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using MySql.Data.MySqlClient;
using static Globals;
using static Globals.DebugLevel;

namespace Mirage.Reporting
{
    public class Reporting
    {
        //=========================================================|
        //  Global Reporting Variables                             |        
        //=========================================================|
        public bool issueReport;
        public int report_id;
        public DateTime start_date;
        public DateTime end_date;

        //=========================================================|
        //  Used For Logging & Debugging                           |     
        //=========================================================|
        private static readonly Type AREA = typeof(Reporting);

        public Reporting() { }

        /// <summary>
        /// Checks if we have to generate a report by scanning the database.
        /// </summary>
        public void checkReportTriggers()
        {
            // Check if we have to generate a report by scanning the database
            try
            {
                issueReport = false;

                string sql = "SELECT * FROM report_settings WHERE PROCESS = 0;";
                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    logger(AREA, DEBUG, "ID: " + rdr.GetInt32(0) + " - Date From: " + rdr.GetString(2) + " - Date To: " + rdr.GetString(3));

                    report_id = rdr.GetInt32(0);
                    start_date = rdr.GetDateTime(2);
                    end_date = rdr.GetDateTime(3);

                    issueReport = true;
                }
            }
            catch
            {
                logger(AREA, ERROR, "Failed To Check DB For Reporting");
                issueReport = false;
            }
        }

        /// <summary>
        /// Generates an SQL query for a given report.
        /// </summary>
        /// <returns>Returns an SQL query as a string</returns>
        public string generateSQLquery()
        {
            string sql;

            // No command to fetch data and generate reports - Do nothing
            logger(AREA, DEBUG, "Generating SQL For Report " + report_id);

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
                sql = "CALL report_2('" + start_date.ToString("yyyy-MM-dd HH:mm") + "')";
            }
            else
            {
                sql = "";
            }

            return sql;
        }

        /// <summary>
        /// Generates and saves first report spreadsheet.
        /// </summary>
        /// <param name="sql">MySQL string; a procedure call to the database</param>
        public void generateReport1(string sql)
        {
            logger(AREA, INFO, "Generating Report 1. Start At " + DateTime.Now.ToString());

            // Declare local variables
            string filename = "";
            string[] headings = { "Robot ID", "Robot IP", "Model", "Name", "Uptime", "Idle Time", "Charging Time", "Mission Time",
                        "Missions Completed", "Jobs Completed", "Average Linear Velocity", "Distance Moved", "Errors Encountered" };
            int spreadsheet_row = 4; // Spreadsheet row at which the sql data starts
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet1 = workbook.CreateSheet("Report 1");
            IRow row = sheet1.CreateRow(0);
            ICellStyle style1 = workbook.CreateCellStyle();

            try
            {
                filename = "reports/MiR Overview Report - ";
                filename += DateTime.Now.ToString("yyyy-MM-dd__HH-mm-ss");
                filename += ".xlsx";
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Create Filename. Error: ", e);
            }

            try
            {
                IFont font1 = workbook.CreateFont();
                font1.Color = IndexedColors.Black.Index;
                font1.IsItalic = false;
                font1.IsBold = true;
                font1.FontHeightInPoints = 15;

                // Bind font1 with style 1
                style1.SetFont(font1);
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Create Font. Error: ", e);
            }

            try
            {
                logger(AREA, DEBUG, "Generating Headline And A Header Row");

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

                logger(AREA, DEBUG, "Created Headline");

                sheet1.CreateRow(2).CreateCell(0).SetCellValue("Robot Data");
                row = sheet1.CreateRow(3);

                for (int i = 0; i < 13; i++)
                {
                    row.CreateCell(i).SetCellValue(headings[i]);
                }

                logger(AREA, DEBUG, "Generated Header Row");
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Generate Headline And Header Row. Error: ", e);
            }

            try
            {
                logger(AREA, DEBUG, "Populating Spreadsheet With SQL Data");
                logger(AREA, DEBUG, "SQL Query: " + sql);

                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

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

                rdr.Close();
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Populate Spreadsheet With SQL Data. Error: ", e);
            }

            try
            {
                logger(AREA, INFO, "Saving To " + filename);

                FileStream sw = File.Create(filename);
                workbook.Write(sw);
                sw.Close();
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Save File. Error: ", e);
            }

            resetDB();

            logger(AREA, INFO, "Report 1 Generated @ " + DateTime.Now.ToString());
        }

        /// <summary>
        /// Generates and saves second report spreadsheet - robot health.
        /// </summary>
        /// <param name="sql">MySQL string; a procedure call to the database</param>
        public void generateReport2(string sql)
        {
            logger(AREA, INFO, "Generating Report 2. Start At " + DateTime.Now.ToString());

            string filename = "reports/MiR Health Report - ";
            filename += DateTime.Now.ToString("yyyy-MM-dd__HH-mm-ss");
            filename += ".xlsx";
            string[] headings = { "Date", "Mode", "State ID", "State", "Battery Time Remaining", "Battery Percentage", "Distance Moved", "Mission", "Missions Text", "Error Code", "Description", "Module" };
            int spreadsheet_row = 3;
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet1 = workbook.CreateSheet("Report 2");
            IRow row = sheet1.CreateRow(0);
            ICellStyle style1 = workbook.CreateCellStyle();

            try
            {
                IFont font1 = workbook.CreateFont();
                font1.Color = IndexedColors.Black.Index;
                font1.IsItalic = false;
                font1.IsBold = true;
                font1.FontHeightInPoints = 15;

                // Bind font1 with style 1
                style1.SetFont(font1);
            }
            catch(Exception e)
            {
                logger(AREA, ERROR, "Failed To Create Font. Error: ", e);
            }

            try
            {
                logger(AREA, DEBUG, "Created Headline");

                ICell cell = row.CreateCell(0);
                cell.SetCellValue("Mirage Data Report 2 - Robot Health Check");
                cell.CellStyle = style1;

                row = sheet1.CreateRow(2);

                for (int i = 0; i < 12; i++)
                {
                    row.CreateCell(i).SetCellValue(headings[i]);
                }

                logger(AREA, DEBUG, "Generated Header Row");
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Generate Headline And Header Row. Error: ", e);
            }

            try
            {
                logger(AREA, DEBUG, "Populating Spreadsheet With SQL Data");
                logger(AREA, DEBUG, "SQL Query: " + sql);

                using var cmd = new MySqlCommand(sql, db);
                using MySqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    logger(AREA, DEBUG, "Result Row: " + (spreadsheet_row - 3));

                    row = sheet1.CreateRow(spreadsheet_row);

                    for (int i = 0; i < 12; i++)
                    {
                        if (rdr.IsDBNull(i))
                        {
                            row.CreateCell(i).SetCellValue("");
                        }
                        else
                        {
                            row.CreateCell(i).SetCellValue(rdr.GetString(i));
                        }
                    }

                    spreadsheet_row++;
                }

                rdr.Close();
            }
            catch (Exception e)
            {
                logger(AREA, ERROR, "Failed To Populate Spreadsheet With SQL Data. Error: ", e);
            }

            try
            {
                logger(AREA, INFO, "Saving To " + filename);

                FileStream sw = File.Create(filename);
                workbook.Write(sw);
                sw.Close();
            }
            catch(Exception e)
            {
                logger(AREA, ERROR, "Failed To Save File. Error: ", e);
            }

            resetDB();

            logger(AREA, INFO, "Report 2 Generated @ " + DateTime.Now.ToString());
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
            if (issueReport)
            {
                string sql = generateSQLquery();

                if (report_id == 1)
                {
                    generateReport1(sql);
                }
                else if (report_id == 2)
                {
                    generateReport2(sql);
                }
            }
            else
            {
                // No Reports Needed
            }
        }
    }
}
