using OneDrive.Logging;
using OneDrive.Native;
using OneDrive.OdSyncService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

#nullable enable
namespace OneDrive {
    public class OneDriveStatus {
        public string? Type { get; set; } = null;
        public bool IncludeLog { get; set; } = false;
        public bool onDemandOnly { get; set; } = false;
        public bool ShowDllPath { get; set; } = false;

        internal const string dllName = "ODNative.dll";
        static string? dllPath = null;
        static string? originalPath = null;

        public OneDriveStatus(string? Type = null, bool IncludeLog = false, bool onDemandOnly = false, bool ShowDllPath = false) {
            this.Type = Type;
            this.IncludeLog = IncludeLog;
            this.onDemandOnly = onDemandOnly;
            this.ShowDllPath = ShowDllPath;
        }

        private void BasicChecks() {
            if (!Environment.UserInteractive && !onDemandOnly)
                throw new InvalidOperationException("Non-Interactive mode detected. OneDrive Status can only be checked interactively unless -OnDemandOnly is specified");
            if (UacHelper.IsProcessElevated && !onDemandOnly)
                throw new InvalidOperationException("UDB is running in Administrator mode. OneDrive status cannot be checked with elevated privileges");
            OdSyncStatusWS.OnDemandOnly = onDemandOnly;
            WriteLog.ShouldLog = IncludeLog;
            if (IncludeLog)
                Debug.WriteLine("Log file is being saved @ " + WriteLog.FileName);
            if (onDemandOnly) {
                Debug.WriteLine("On Demand Only check");
                WriteLog.WriteInformationEvent("On Demand Only option selected");
            }
            if (dllPath is null) {
                CopyDLL();
            }
            if (ShowDllPath) {
                Debug.WriteLine("Show DLL folder is enabled");
                Debug.WriteLine($"The temporary DLL path is '{Path.Combine(dllPath, dllName)}'");
            }
        }
        public ServiceStatus GetStatus (string TargetPath) {
            BasicChecks();
            OdSyncStatusWS os = new OdSyncStatusWS();
            WriteLog.WriteInformationEvent($"Path being tested is '{TargetPath}'");
            return os.GetStatus(TargetPath);
        }
        public bool GetStatus (string TargetPath, Guid CLSID) {
            BasicChecks();
            WriteLog.WriteInformationEvent($"Path being tested is '{TargetPath}'");
            return API.IsCertainType(TargetPath, CLSID);
        }
        public List<StatusDetail> GetStatus (Guid CLSID = new Guid()) {
            BasicChecks();
            OdSyncStatusWS os = new OdSyncStatusWS();
            StatusDetailCollection statusCol = os.GetStatus();
            List<StatusDetail> statuses = new List<StatusDetail>();
            foreach(StatusDetail status in statusCol) {
                if (Type is null || status.ServiceType.ToLower().Contains(Type.ToLower().Replace("*", ""))) {
                    WriteLog.WriteInformationEvent($"Guid Type being tested is '{CLSID}'");
                    if (CLSID != Guid.Empty)
                        status.StatusString = API.IsCertainType(status.LocalPath, CLSID) ? "GuidFound " + CLSID.ToString("B") : "GuidNotFound " + CLSID.ToString("B");
                }
                statuses.Add(status);
            }
            return statuses;
        }
        private static void CopyDLL() {
            dllPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            try {
                Directory.CreateDirectory(dllPath);
            } catch (Exception ex) {
                throw new Exception(String.Format("Unable to generate folder for support files at {0}\n{1}", dllPath, ex.ToString()));
            }
            byte[] streamBytes;
            if (Marshal.SizeOf(new IntPtr()) == 8) // 64 bits
            {
                streamBytes = Properties.Resources.ODNative64;
            } else {
                streamBytes = Properties.Resources.ODNative32;
            }
            try {
                using (Stream fileStream = File.OpenWrite(Path.Combine(dllPath, dllName))) {
                    fileStream.Write(streamBytes, 0, streamBytes.Length);
                }
            } catch (Exception ex) {
                string tmpStr = dllPath;
                dllPath = null;
                throw new Exception(String.Format("Unable to generate support files at {0}\n{1}", tmpStr, ex.ToString()));
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
#nullable disable
