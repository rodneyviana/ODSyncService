using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace OdSyncService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IOdSyncStatusWS" in both code and config file together.
    [ServiceContract]
    public interface IOdSyncStatusWS
    {
        [OperationContract]
        StatusDetailCollection GetStatus();

    }

    [DataContract]
    public enum ServiceStatus
    {
        Error,
        Shared,
        SharedSync,
        UpToDate,
        Syncing,
        ReadOnly,
        OnDemandOrUnknown
    }

    public struct QuotaColor
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;
        public QuotaColor(byte A, byte R, byte G, byte B)
        {
            this.A = A;
            this.R = R;
            this.G = G;
            this.B = B;
        }
    };

    public class PathStatus
    {
        internal PathStatus(string FilePath, ServiceStatus FileStatus)
        {
            Path = FilePath;
            Status = FileStatus.ToString();
        }

        public string Path
        {
            protected set;
            get;
        }

        public string Status
        {
            protected set;
            get;
        }
    }

    [DataContract]
    public class StatusDetail
    {
        [DataMember]
        public string SyncRootId;

        [DataMember]
        public string LocalPath;

        [DataMember]
        public string UserSID;

        [DataMember]
        public string UserName;

        internal ServiceStatus Status;

        [DataMember]
        public int NewApiStatus = -1;

        [DataMember]
        public string DisplayName;

        [DataMember]
        public string ServiceType;

        [DataMember]
        public string StatusString;

        [DataMember]
        public string IconPath;

        [DataMember]
        public string QuotaLabel;

        [DataMember]
        public ulong QuotaTotalBytes = 0;

        [DataMember]
        public ulong QuotaUsedBytes = 0;

        [DataMember]
        public QuotaColor QuotaColor = new QuotaColor(0, 0, 0, 0);

        [DataMember]
        public bool IsNewApi = false;

        public List<PathStatus> GetUnsynchedFiles(bool StopAtFirst = true)
        {
            string[] files = Directory.GetFiles(LocalPath, "*.*", SearchOption.AllDirectories);
            OdSyncStatusWS os = new OdSyncStatusWS();
            List<PathStatus> synched = new List<PathStatus>();
            int i = 0;
            foreach (var file in files)
            {
                var status = os.GetStatus(file);
                
                if(status != ServiceStatus.OnDemandOrUnknown && status != ServiceStatus.UpToDate && status != ServiceStatus.SharedSync)
                {
                    synched.Add(new PathStatus(file, status));
                    if(StopAtFirst)
                    {
                        return synched;
                    }
                    if (i++ == 100)
                    {
                        i = 0;
                        Thread.Sleep(0); // let the cpu process other threads
                    }
                }
            }
            return synched;
        }
    }

    [CollectionDataContract]
    public class StatusDetailCollection : List<StatusDetail>
    {
        public StatusDetailCollection()
            : base()
        {
        }

        public StatusDetailCollection(List<StatusDetail> items)
            : base()
        {
            foreach (var item in items)
            {
                Add(item);
            }
        }
    }

}
