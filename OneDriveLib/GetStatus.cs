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
        [Parameter(Position = 0)]
        public string Type
        {
            get; set;
        }

        [Parameter(Position = 1)]
        public string ByPath
        {
            get; set;
        }

        static string dllPath = null;
        static string originalPath = null;

        protected override void ProcessRecord()
        {
            if (dllPath == null)
            {
                dllPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                try
                {
                    Directory.CreateDirectory(dllPath);
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Unable to generate folder for support files at {0}\n{1}",dllPath, ex.ToString()));

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
                    using (Stream fileStream = File.OpenWrite(Path.Combine(dllPath, "ODNative.dll")))
                    {
                        fileStream.Write(streamBytes, 0, streamBytes.Length);
                    }
                } catch (Exception ex)
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

            OdSyncStatusWS os = new OdSyncStatusWS();
            List<StatusDetail> statuses = new List<StatusDetail>();

            // Just Get the Path
            if(!String.IsNullOrEmpty(ByPath))
            {
                WriteObject(os.GetStatus(ByPath).ToString());
                return;
                
            }
            var statusCol = os.GetStatus();
            foreach(var status in statusCol)
            {
                if (String.IsNullOrEmpty(Type) || status.ServiceType.ToLower().Contains(Type.ToLower().Replace("*", "")))
                {
                    statuses.Add(status);

                }

            }
            WriteObject(statuses.ToArray());
        }
    }
}
