using System;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace OneDrive.Logging {
    public static class WriteLog {
        private static readonly object writeLock = new object();
        private static DateTime failedCreate = DateTime.MinValue;
        public const string Source = "OneDriveLib";
        public const string Log = "Application";
        public static bool WriteToFile { get; set; }
        public static string FileName {
            get {
                return String.Format("{0}OneDriveLib-{1}.log", Path.GetTempPath(), DateTime.Now.ToString("yyy-MM-dd"));
            }
        }
        public static bool ShouldLog { get; set; } = false;
        public static void AppendToFile(EventLogEntryType Severity, string Message) {
            AppendToFile(String.Format("{0}\t{1}\t{2}",
                DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss"),
                Severity,
                Message));
        }
        public static void AppendToFile(string Text) {
            //byte[] strArray = Encoding.UTF8.GetBytes(Text);
            AppendToFile(Encoding.UTF8.GetBytes(Text));
        }
        public static void AppendToFile(byte[] RawBytes) {
            bool myLock = System.Threading.Monitor.TryEnter(writeLock, 100);
            if (myLock) {
                try {
                    using (FileStream stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite)) {
                        stream.Position = stream.Length;
                        stream.Write(RawBytes, 0, RawBytes.Length);
                        stream.WriteByte(13);
                        stream.WriteByte(10);
                    }
                } catch (Exception ex) {
                    string str = string.Format("Unable to create log. Type: {0} Message: {1}\nStack:{2}", ex, ex.Message, ex.StackTrace);
                    Debug.WriteLine(str);
                    Debug.Flush();
                } finally {
                    System.Threading.Monitor.Exit(writeLock);
                }
            }
        }
        public static bool CreateSource() {
            return CreateSource(Source);
        }
        internal static bool CreateSource(string Source) {
            try {
                EventLog.CreateEventSource(Source, Log);
                return true;
            } catch (Exception ex) {
                if (DateTime.Now.Subtract(failedCreate) > TimeSpan.FromHours(1)) {
                    AppendToFile(EventLogEntryType.Error, String.Format("Unable to create Event Log Source '0' - {1}:{2}",
                        Source,
                        ex.GetType(),
                        ex.Message));
                }
                failedCreate = DateTime.Now;
            }
            return false;
        }
        internal static bool WriteToLog(EventLogEntryType Severity, string Message) {
            if (!ShouldLog)
                return false;
            if (WriteToFile) {
                AppendToFile(Severity, Message);
                return true;
            }
            bool sourceCreated;
            try {
                sourceCreated = EventLog.SourceExists(Source);
            } catch {
                sourceCreated = false;
            }
            if (!sourceCreated)
                sourceCreated = CreateSource();
            if (sourceCreated) {
                EventLog.WriteEntry(Source, Message, Severity);
                return true;
            } else {
                AppendToFile(Severity, Message);
            }
            return false;
        }
        public static bool WriteInformationEvent(string Message) {
            return WriteToLog(EventLogEntryType.Information, Message);
        }
        public static bool WriteErrorEvent(string Message) {
            return WriteToLog(EventLogEntryType.Error, Message);
        }
        public static bool WriteWarningEvent(string Message) {
            return WriteToLog(EventLogEntryType.Warning, Message);
        }
    }
}
