using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.DirectoryServices.AccountManagement;
using OneDrive.Native;
using OneDrive.Logging;

#nullable enable
namespace OneDrive.OdSyncService {
    class OdSyncStatusWS : IOdSyncStatusWS {
        private static string? userSID = null;
        internal static bool OnDemandOnly { get; set; } = false;
        private static string UserSID {
            get {
                userSID ??= WindowsIdentity.GetCurrent().User.ToString();
                return userSID;
            }
        }
        public ServiceStatus GetStatus(string Path) {
            if (!OnDemandOnly) {
                if (API.IsTrue<IIconError>(Path))
                    return ServiceStatus.Error;
                if (API.IsTrue<IIconUpToDate>(Path))
                    return ServiceStatus.UpToDate;
                if (API.IsTrue<IIconReadOnly>(Path))
                    return ServiceStatus.ReadOnly;
                if (API.IsTrue<IIconShared>(Path))
                    return ServiceStatus.Shared;
                if (API.IsTrue<IIconSharedSync>(Path))
                    return ServiceStatus.SharedSync;
                if (API.IsTrue<IIconSync>(Path))
                    return ServiceStatus.Syncing;
                if (API.IsTrue<IIconGrooveUpToDate>(Path))
                    return ServiceStatus.UpToDate;
                if (API.IsTrue<IIconGrooveSync>(Path))
                    return ServiceStatus.Syncing;
                if (API.IsTrue<IIconGrooveError>(Path))
                    return ServiceStatus.Error;
            }
            return ServiceStatus.OnDemandOrUnknown;
        }

        public IEnumerable<StatusDetail> GetStatusInternal() {
            const string subkeyString = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager\";
            using RegistryKey key = Registry.LocalMachine.OpenSubKey(subkeyString);
            if (key is null) {
                yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown };
            } else {
                if (key.SubKeyCount == 0) {
                    yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown, ServiceType = "OneDrive" };
                }
                foreach (string subkey in key.GetSubKeyNames()) {
                    RegistryKey displayKey = key.OpenSubKey(subkey);
                    string? displayName = displayKey.GetValue("DisplayNameResource") as string;
                    using RegistryKey userKey = key.OpenSubKey(String.Format("{0}{1}", subkey, @"\UserSyncRoots"));
                    if (userKey != null && userKey.Name.Contains(UserSID)) {
                        foreach (string valueName in userKey.GetValueNames()) {
                            StatusDetail detail = new StatusDetail();
                            try {
                                SecurityIdentifier id = new SecurityIdentifier(valueName);
                                //string userName = id.Translate(typeof(NTAccount)).Value;
                                detail.UserName = id.Translate(typeof(NTAccount)).Value;
                                detail.UserSID = valueName;
                                detail.DisplayName = displayName;
                                detail.SyncRootId = subkey;

                                string[] parts = userKey.Name.Split('!');

                                if (parts.Length > 1) {
                                    detail.ServiceType = parts[Math.Min(2, parts.Length - 1)].Split('|')[0];
                                } else {
                                    detail.ServiceType = "INVALID";
                                }
                            } catch (Exception ex) {
                                detail.UserName = String.Format("{0}: {1}", ex.GetType().ToString(), ex.Message);
                                WriteLog.WriteErrorEvent("OneDrive " + detail.UserName);
                            }
                            detail.LocalPath = userKey.GetValue(valueName) as string;
                            detail.StatusString = GetStatus(detail.LocalPath!).ToString();
                            yield return detail;
                        }
                    }
                }
            }
        }
        public IEnumerable<StatusDetail> GetStatusInternalGroove() {
            const string subkeyString = @"Software\Microsoft\Office";
            using RegistryKey key = Registry.CurrentUser.OpenSubKey(subkeyString);
            if (key == null) {
                yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown, ServiceType = "Groove" };
            } else {
                if (key.SubKeyCount == 0) {
                    yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown };
                }
                foreach (string subkey in key.GetSubKeyNames()) {
                    using RegistryKey userKey = key.OpenSubKey(String.Format("{0}{1}", subkey, @"\Common\Internet"));
                    if (userKey != null && userKey.GetValue("LocalSyncClientDiskLocation") as String[] != null) {
                        string[] folders = userKey.GetValue("LocalSyncClientDiskLocation") as String[] ?? new string[0];
                        foreach (string folder in folders) {
                            StatusDetail detail = new StatusDetail();
                            try {
                                detail.UserName = WindowsIdentity.GetCurrent().Name;
                                detail.UserSID = UserPrincipal.Current.Sid.ToString();
                                string[] parts = subkey.Split('!');
                                detail.ServiceType = String.Format("Groove{0}", parts[parts.Length - 1]);
                            } catch (Exception ex) {
                                detail.UserName = String.Format("Groove - {0}: {1}", ex.GetType().ToString(),
                                    ex.Message);
                                Logging.WriteLog.WriteErrorEvent(detail.UserName);
                            }
                            detail.LocalPath = folder;
                            detail.StatusString = GetStatus(detail.LocalPath).ToString();
                            yield return detail;
                        }
                    }

                }
            }
        }
        public StatusDetailCollection GetStatus() {
            WriteLog.WriteToFile = true;
            WriteLog.WriteInformationEvent(String.Format("Is Interactive: {0}, Is UAC Enabled: {1}, Is Elevated: {2}", Environment.UserInteractive, UacHelper.IsUacEnabled,
                OneDrive.UacHelper.IsProcessElevated));
            StatusDetailCollection statuses = new StatusDetailCollection();
            foreach (StatusDetail status in GetStatusInternal()) {
                uint hr = API.GetStateBySyncRootId(status.SyncRootId, out OneDriveState state);
                if (hr == 0) {
                    status.QuotaUsedBytes = state.UsedQuota;
                    status.QuotaTotalBytes = state.TotalQuota;
                    status.NewApiStatus = state.CurrentState;
                    status.StatusString = state.CurrentState == 0 ? "Synced" : state.Label;
                    status.QuotaLabel = state.QuotaLabel;
                    status.QuotaColor = new QuotaColor(state.IconColorA, state.IconColorR, state.IconColorG, state.IconColorB);
                    status.IconPath = state.IconUri;
                    status.IsNewApi = true;
                    statuses.Add(status);
                }
                if (hr != 0 && status.Status != ServiceStatus.OnDemandOrUnknown) {
                    if (status.Status == ServiceStatus.Error) {
                        status.StatusString = API.GetStatusByDisplayName(status.DisplayName);
                    }
                    statuses.Add(status);
                }
            }
            foreach (var status in GetStatusInternalGroove()) {
                if (status.Status != ServiceStatus.OnDemandOrUnknown) {
                    if (status.Status == ServiceStatus.Error) {
                        status.StatusString = API.GetStatusByDisplayName(status.DisplayName);
                    }
                    statuses.Add(status);
                }
            }
            return statuses;
        }
    }
}
#nullable disable