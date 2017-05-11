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

            return ServiceStatus.NotInstalled;
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
        public StatusDetailCollection GetStatus()
        {
            StatusDetailCollection statuses = new StatusDetailCollection();

            foreach (var status in GetStatusInternal())
                statuses.Add(status);
            return statuses;
        }



    }
}
