using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OdSyncService;
using System.Management.Automation;
using System.IO;
using System.Runtime.InteropServices;

namespace OneDriveLib
{
    [Cmdlet(VerbsCommon.Get, "ODStatus")]
    public class GetStatus: Cmdlet
    {
        [Parameter(Position = 0,HelpMessage = "Type of service (e.g.: Business1 or Personal")]
        public string Type
        {
            get; set;
        }

        [Parameter(Position = 1,HelpMessage = "To test the status of a specific path")]
        public string ByPath
        {
            get; set;
        }

        [Parameter(Position = 2, HelpMessage = "To test a specific Icon overlay (for other services like Dropbox)")]
        public Guid CLSID
        {
            get; set;
        }

        [Parameter(Position = 3, HelpMessage = "Create a log file")]
        public bool IncludeLog
        {
            get; set;
        }

        private const string dllName = "ODNative.dll";
        static string dllPath = null;
        static string originalPath = null;

        protected override void ProcessRecord()
        {
            if (!Environment.UserInteractive)
                throw new InvalidOperationException("Non-Interactive mode detected. OneDrive Status can only be checked interactively");
            if (UacHelper.IsProcessElevated)
                throw new InvalidOperationException("PowerShell is running in Administrator mode. OneDrive status cannot be checked in elevated privileges");
            WriteLog.ShouldLog = IncludeLog;
            if (IncludeLog)
                WriteVerbose("Log file is being saved @ "+WriteLog.FileName);
            if (dllPath == null)
            {
                CopyDLL();
            }

            OdSyncStatusWS os = new OdSyncStatusWS();
            List<StatusDetail> statuses = new List<StatusDetail>();

            // Just Get the Path
            if(!String.IsNullOrEmpty(ByPath))
            {
                if(CLSID == Guid.Empty)
                    WriteObject(os.GetStatus(ByPath).ToString());
                else
                {

                    WriteObject(Native.API.IsCertainType(ByPath, CLSID));
                }
                return;
                
            }
            var statusCol = os.GetStatus();
            foreach(var status in statusCol)
            {
                if (String.IsNullOrEmpty(Type) || status.ServiceType.ToLower().Contains(Type.ToLower().Replace("*", "")))
                {
                    if(CLSID != Guid.Empty)
                    {
                        status.StatusString = Native.API.IsCertainType(status.LocalPath, CLSID) ? "GuidFound "+CLSID.ToString("B") : "GuidNotFound " + CLSID.ToString("B");
                    }
                    statuses.Add(status);

                }

            }
            WriteObject(statuses.ToArray());
            // Cleanup if possible
            try
            {
                File.Delete(Path.Combine(dllPath, dllName));
            } catch
            {

            }
        }

        private static void CopyDLL()
        {
            dllPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try
            {
                Directory.CreateDirectory(dllPath);
            }
            catch (Exception ex)
            {
                throw new Exception(String.Format("Unable to generate folder for support files at {0}\n{1}", dllPath, ex.ToString()));

            }
            byte[] streamBytes = null;
            if (Marshal.SizeOf(new IntPtr()) == 8) // 64 bits
            {
                streamBytes = Properties.Resources.ODNative64;
            }
            else
            {
                streamBytes = Properties.Resources.ODNative32;
            }
            try
            {
                using (Stream fileStream = File.OpenWrite(Path.Combine(dllPath, dllName)))
                {
                    fileStream.Write(streamBytes, 0, streamBytes.Length);
                }
            }
            catch (Exception ex)
            {
                string tmpStr = dllPath;
                dllPath = null;
                throw new Exception(String.Format("Unable to generate support files at {0}\n{1}", dllPath, ex.ToString()));
            }
            // Set up search path DLL
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            if (originalPath == null)
                originalPath = path;
            path += ";" + dllPath;

            Environment.SetEnvironmentVariable("PATH", path);
        }
    }
}
