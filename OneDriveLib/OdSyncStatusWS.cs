using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.ServiceModel;
using System.Text;
using Native;
using System.DirectoryServices.AccountManagement;
//using FileSyncLibrary;

namespace OdSyncService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "OdSyncStatusWS" in both code and config file together.
    public class OdSyncStatusWS : IOdSyncStatusWS
    {
        /*
        public string GetSystemStatus(string Path)
        {
            try
            {
                ItemStatus status;

                FileSyncClientClass fs = new FileSyncClientClass();

                fs.GetItemStatus(Path, out status);
                return status.ToString();
            }
            catch (Exception ex)
            {
                return String.Format("Error: {0}: {1}", ex.GetType().ToString(), ex.Message);
            }

        }
        */

        public ServiceStatus GetStatus(string Path)
        {


            if (Native.API.IsTrue<IIconError>(Path))
                return ServiceStatus.Error;
            if (Native.API.IsTrue<IIconUpToDate>(Path))
                return ServiceStatus.UpToDate;
            if (Native.API.IsTrue<IIconReadOnly>(Path))
                return ServiceStatus.ReadOnly;
            if (Native.API.IsTrue<IIconShared>(Path))
                return ServiceStatus.Shared;
            if (Native.API.IsTrue<IIconSharedSync>(Path))
                return ServiceStatus.SharedSync;
            if (Native.API.IsTrue<IIconSync>(Path))
                return ServiceStatus.Syncing;
            if (Native.API.IsTrue<IIconGrooveUpToDate>(Path))
                return ServiceStatus.UpToDate;
            if (Native.API.IsTrue<IIconGrooveSync>(Path))
                return ServiceStatus.Syncing;
            if (Native.API.IsTrue<IIconGrooveError>(Path))
                return ServiceStatus.Error;

            return ServiceStatus.OnDemandOrUnknown;
        }

        /*
        public IEnumerable<StatusDetail> GetStatusInternal()
        {
            //const string hklm = "HKEY_LOCAL_MACHINE";
            const string subkeyString = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager\"; // SkyDrive\UserSyncRoots\";

            using (var key = Registry.LocalMachine.OpenSubKey(subkeyString))
            {
                if (key == null)
                {
                    yield return new StatusDetail() { Status = ServiceStatus.NotInstalled };
                }
                else
                {
                    if (key.SubKeyCount == 0)
                    {
                        yield return new StatusDetail() { Status = ServiceStatus.NotInstalled };
                    }
                    foreach (var subkey in key.GetSubKeyNames())
                    {
                        using (var userKey = key.OpenSubKey(String.Format("{0}{1}", subkey, @"\UserSyncRoots")))
                        {
                            if (userKey != null)
                            {
                                foreach (var valueName in userKey.GetValueNames())
                                {
                                    var detail = new StatusDetail();
                                    try
                                    {
                                        var id = new SecurityIdentifier(valueName);
                                        string userName = id.Translate(typeof(NTAccount)).Value;
                                        detail.UserName = userName;
                                        detail.UserSID = valueName;
                                    }
                                    catch (Exception ex)
                                    {
                                        detail.UserName = String.Format("{0}: {1}", ex.GetType().ToString(),
                                            ex.Message);
                                    }
                                    detail.LocalPath = userKey.GetValue(valueName) as string;
                                    detail.StatusString = GetSystemStatus(detail.LocalPath);
                                    yield return detail;
                                }
                            }
                        }
                    }
                }
            }



        }
        */


        private IEnumerable<StatusDetail> GetStatusInternal(bool currentUserOnly)
        {
            //const string hklm = "HKEY_LOCAL_MACHINE";
            const string subkeyString = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\SyncRootManager\"; // SkyDrive\UserSyncRoots\";

            using (var key = Registry.LocalMachine.OpenSubKey(subkeyString))
            {
                if (key == null)
                {
                    yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown };
                }
                else
                {
                    if (key.SubKeyCount == 0)
                    {
                        yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown, ServiceType="OneDrive" };
                    }

                    IEnumerable<string> subKeys = key.GetSubKeyNames();
                    if (currentUserOnly)
                    {
                        var currentUser = WindowsIdentity.GetCurrent().User?.Value ?? string.Empty;
                        subKeys = subKeys.Where(s => s.Contains(currentUser));
                    }

                    foreach (var subKey in subKeys)
                    {
                        using (var userKey = key.OpenSubKey(String.Format("{0}{1}", subKey, @"\UserSyncRoots")))
                        {
                            if (userKey != null)
                            {
                                foreach (var valueName in userKey.GetValueNames())
                                {
                                    var detail = new StatusDetail();
                                    try
                                    {
                                        var id = new SecurityIdentifier(valueName);
                                        string userName = id.Translate(typeof(NTAccount)).Value;
                                        detail.UserName = userName;
                                        detail.UserSID = valueName;

                                        
                                        string[] parts = userKey.Name.Split('!');

                                        if (parts.Length > 1)
                                        {
                                            detail.ServiceType = parts[Math.Min(2, parts.Length - 1)].Split('|')[0];
                                        } else
                                        {
                                            detail.ServiceType = "INVALID";
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        detail.UserName = String.Format("{0}: {1}", ex.GetType().ToString(),
                                            ex.Message);
                                    }
                                    detail.LocalPath = userKey.GetValue(valueName) as string;
                                    detail.StatusString = GetStatus(detail.LocalPath).ToString();
                                    yield return detail;
                                }
                            }
                        }
                    }
                }
            }



        }

        public IEnumerable<StatusDetail> GetStatusInternalGroove()
        {
            //const string hklm = "HKEY_LOCAL_MACHINE";
            const string subkeyString = @"Software\Microsoft\Office"; // SkyDrive\UserSyncRoots\";

            using (var key = Registry.CurrentUser.OpenSubKey(subkeyString))
            {
                if (key == null)
                {
                    yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown, ServiceType="Groove" };
                }
                else
                {
                    if (key.SubKeyCount == 0)
                    {
                        yield return new StatusDetail() { Status = ServiceStatus.OnDemandOrUnknown };
                    }
                    foreach (var subkey in key.GetSubKeyNames())
                    {
                        using (var userKey = key.OpenSubKey(String.Format("{0}{1}", subkey, @"\Common\Internet")))
                        {
                            if (userKey != null && userKey.GetValue("LocalSyncClientDiskLocation") as String[] != null)
                            {
                                string[] folders = userKey.GetValue("LocalSyncClientDiskLocation") as String[];
                                foreach (var folder in folders)
                                {
                                    var detail = new StatusDetail();
                                    try
                                    {

                                        detail.UserName = WindowsIdentity.GetCurrent().Name;
                                        detail.UserSID = UserPrincipal.Current.Sid.ToString();


                                        string[] parts = subkey.Split('!');

                                        detail.ServiceType = String.Format("Groove{0}", parts[parts.Length - 1]);

                                    }
                                    catch (Exception ex)
                                    {
                                        detail.UserName = String.Format("{0}: {1}", ex.GetType().ToString(),
                                            ex.Message);
                                    }
                                    detail.LocalPath = folder;
                                    detail.StatusString = GetStatus(detail.LocalPath).ToString();
                                    yield return detail;
                                }
                            }
                        }

                    }
                }
            }



        }
        public StatusDetailCollection GetStatus(bool currentUserOnly = false)
        {
            OneDriveLib.WriteLog.WriteToFile = true;
            OneDriveLib.WriteLog.WriteInformationEvent(String.Format("Is Interactive: {0}, Is UAC Enabled: {1}, Is Elevated: {2}", Environment.UserInteractive, OneDriveLib.UacHelper.IsUacEnabled,
                OneDriveLib.UacHelper.IsProcessElevated));

            StatusDetailCollection statuses = new StatusDetailCollection();

            foreach (var status in GetStatusInternal(currentUserOnly))
                if(status.Status != ServiceStatus.OnDemandOrUnknown)
                    statuses.Add(status);
            foreach (var status in GetStatusInternalGroove())
                if (status.Status != ServiceStatus.OnDemandOrUnknown)
                    statuses.Add(status);
            return statuses;
        }



    }
}
