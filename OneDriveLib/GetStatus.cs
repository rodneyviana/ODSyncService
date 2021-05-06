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
    [Cmdlet(VerbsCommon.Get, "ODStatus", DefaultParameterSetName = "Regular")]
    public class GetStatus: PSCmdlet
    {
        [Parameter(HelpMessage = "Type of service (e.g.: Business1 or Personal")]
        public string Type
        {
            get; set;
        }

        [Parameter(HelpMessage = "To test the status of a specific path",ParameterSetName = "Regular")]
        public string ByPath
        {
            get; set;
        }

        [Parameter(HelpMessage = "To test a specific Icon overlay (for other services like Dropbox)", ParameterSetName = "Regular")]
        public Guid CLSID
        {
            get; set;
        }

        private bool includeLog = false;
        [Parameter(HelpMessage = "Create a log file")]
        public SwitchParameter IncludeLog
        {
            get { return includeLog; }
            set { includeLog = value; }
        }

        private bool onDemandOnly = false;
        [Parameter(HelpMessage = "Skip check for non-OnDemand and only gets the OnDemand status", ParameterSetName = OnDemandString)]
        public SwitchParameter OnDemandOnly
        {
            get { return onDemandOnly; }
            set { onDemandOnly = value; }
        }

        private bool showDllPath = false;
        [Parameter(HelpMessage = "Show the temporary native DLL path. You may want to delete it after unloading the process", ParameterSetName = OnDemandString)]
        public SwitchParameter ShowDllPath
        {
            get { return showDllPath; }
            set { showDllPath = value; }
        }
        internal const string dllName = "ODNative.dll";
        private const string OnDemandString = "OnDemand";
        static string dllPath = null;
        static string originalPath = null;
        // private bool disposedValue;

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            if (!Environment.UserInteractive && ParameterSetName != OnDemandString)
                throw new InvalidOperationException("Non-Interactive mode detected. OneDrive Status can only be checked interactively unless -OnDemandOnly is specified");
            if (UacHelper.IsProcessElevated && ParameterSetName != OnDemandString)
                throw new InvalidOperationException("PowerShell is running in Administrator mode. OneDrive status cannot be checked in elevated privileges");
            OdSyncStatusWS.OnDemandOnly = onDemandOnly;
            WriteLog.ShouldLog = includeLog;
            if (includeLog)
                WriteVerbose("Log file is being saved @ "+WriteLog.FileName);
            if (onDemandOnly)
            {
                WriteVerbose("On Demand Only check");
                WriteLog.WriteInformationEvent("On Demand Only option selected");
            }
            if (showDllPath)
                WriteVerbose("Show DLL folder is enabled");
            if (dllPath == null)
            {
                CopyDLL();
            }

            if(showDllPath)
                Host.UI.WriteLine($"The temporary DLL path is '{Path.Combine(dllPath, dllName)}'");
            OdSyncStatusWS os = new OdSyncStatusWS();
            List<StatusDetail> statuses = new List<StatusDetail>();

            // Just Get the Path
            if(!String.IsNullOrEmpty(ByPath))
            {
                WriteLog.WriteInformationEvent($"Path being tested is '{ByPath}'");
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
                    WriteLog.WriteInformationEvent($"Guid Type being tested is '{CLSID}'");
                    if (CLSID != Guid.Empty)
                    {
                        status.StatusString = Native.API.IsCertainType(status.LocalPath, CLSID) ? "GuidFound "+CLSID.ToString("B") : "GuidNotFound " + CLSID.ToString("B");
                    }
                    statuses.Add(status);

                }
                
            }
            WriteObject(statuses.ToArray());

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

        /*
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (dllPath != null && File.Exists(Path.Combine(dllPath, dllName)))
                {
                    bool removed = false;
                    try
                    {
                        removed = Native.API.UnloadModule();
                        //if (removed)
                        //    WriteVerbose($"DLL '{Path.Combine(dllPath, dllName)}' removed from memory");
                        //else
                        //    WriteVerbose($"DLL '{Path.Combine(dllPath, dllName)}' NOT removed from memory");

                        File.Delete(Path.Combine(dllPath, dllName));
                        DirectoryInfo di = new DirectoryInfo(dllPath);

                        //WriteVerbose($"DLL '{Path.Combine(dllPath, dllName)}' removed successfully");
                        //Environment.SetEnvironmentVariable("PATH", originalPath);
                        dllPath = null;
                        try
                        {
                            di.Delete(true);
                            //WriteVerbose($"Folder '{dllPath}' removed successfully");
                        }
                        catch (Exception ex)
                        {
                            //WriteWarning($"Folder '{dllPath}' could not be removed. Error {ex.ToString()}");
                            //WriteLog.WriteWarningEvent($"Folder '{dllPath}' could not be removed. Error {ex.ToString()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        //WriteLog.WriteWarningEvent($"DLL '{Path.Combine(dllPath, dllName)}' could not be removed. Error: {ex.ToString()}");
                        //WriteWarning($"DLL '{Path.Combine(dllPath, dllName)}' could not be removed. Error: {ex.ToString()}");
                    }
                }

                disposedValue = true;
            }
        }
        */

        /*
        ~GetStatus()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            //Dispose(disposing: false);
            if (dllPath != null && File.Exists(Path.Combine(dllPath, dllName)))
            {
                bool removed = false;
                try
                {
                    removed = Native.API.UnloadModule();
                    //if (removed)
                    //    WriteVerbose($"DLL '{Path.Combine(dllPath, dllName)}' removed from memory");
                    //else
                    //    WriteVerbose($"DLL '{Path.Combine(dllPath, dllName)}' NOT removed from memory");

                    File.Delete(Path.Combine(dllPath, dllName));
                    DirectoryInfo di = new DirectoryInfo(dllPath);

                    //WriteVerbose($"DLL '{Path.Combine(dllPath, dllName)}' removed successfully");
                    //Environment.SetEnvironmentVariable("PATH", originalPath);
                    dllPath = null;
                    try
                    {
                        di.Delete(true);
                        //WriteVerbose($"Folder '{dllPath}' removed successfully");
                    }
                    catch (Exception ex)
                    {
                        //WriteWarning($"Folder '{dllPath}' could not be removed. Error {ex.ToString()}");
                        //WriteLog.WriteWarningEvent($"Folder '{dllPath}' could not be removed. Error {ex.ToString()}");
                    }
                }
                catch (Exception ex)
                {
                    //WriteLog.WriteWarningEvent($"DLL '{Path.Combine(dllPath, dllName)}' could not be removed. Error: {ex.ToString()}");
                    //WriteWarning($"DLL '{Path.Combine(dllPath, dllName)}' could not be removed. Error: {ex.ToString()}");
                }
            }
        }
        */

    }
}
