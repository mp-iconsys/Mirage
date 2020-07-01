using System;
using System.Diagnostics;

namespace Mirage
{
    // Standard Logger for events, etc
    public static class Logger
    {
        public static EventLog eventLog1 = new EventLog();
        public static EventLogTraceListener myTraceListener = new EventLogTraceListener("Mirage");

        public static void ConfigureLogger()
        {
            string eventSourceName = "Mirage";
            string logName = "Mirage";

            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName, logName);
            }

            eventLog1.Source = eventSourceName;
            eventLog1.Log = logName;

            Trace.Listeners.Add(myTraceListener);
        }

        public static void Error(string message, string module) 
        {
            //WriteEntry(message, "error", module);
            //Trace.TraceWarning((message + " In Module " + module));
            eventLog1.WriteEntry((message + " In Module " + module), EventLogEntryType.Error, 0);
        }

        public static void Error(Exception ex, string module)
        {
            //WriteEntry(ex.Message, "error", module);
            //Trace.TraceWarning((ex.Message + " In Module " + module));
            eventLog1.WriteEntry((ex.Message + " In Module " + module), EventLogEntryType.Error, 0);
        }

        public static void Warning(string message, string module)
        {
            //WriteEntry(message, "warning", module);
            //Trace.TraceWarning((message + " In Module " + module));
            eventLog1.WriteEntry((message + " In Module " + module), EventLogEntryType.Warning, 0);
        }

        public static void Info(string message, string module)
        {
            //WriteEntry(message, "info", module);
            //Trace.TraceInformation( (message + " In Module " + module) );
            eventLog1.WriteEntry((message + " In Module " + module), EventLogEntryType.Information, 0);
        }

        private static void WriteEntry(string message, string type, string module)
        {
            //Trace.WriteLine(string.Format("{0},{1},{2},{3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), type, module, message));
        }
    }
}
