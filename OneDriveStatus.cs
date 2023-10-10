using OneDrive.Logging;
using OneDrive.Native;
using OneDrive.OdSyncService;
using System;
using System.Collections.Generic;
using System.Diagnostics;

#nullable enable
namespace OneDrive {
    public class OneDriveStatus {
        public string? Type { get; set; } = null;
        public bool IncludeLog { get; set; } = false;
        public bool onDemandOnly { get; set; } = false;

        public OneDriveStatus(string? Type = null, bool IncludeLog = false, bool onDemandOnly = false) {
            this.Type = Type;
            this.IncludeLog = IncludeLog;
            this.onDemandOnly = onDemandOnly;
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
    }
}
#nullable disable
