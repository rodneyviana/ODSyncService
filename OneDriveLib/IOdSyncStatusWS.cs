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
        StatusDetailCollection GetStatus(bool currentUserOnly = false);

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
        public string LocalPath;

        [DataMember]
        public string UserSID;

        [DataMember]
        public string UserName;


        internal ServiceStatus Status;

        internal string statusString;

        [DataMember]
        public string ServiceType;

        [DataMember]
        public string StatusString
        {
            get
            {
                return statusString;
                //return Status.ToString();
            }
            set
            {
                try
                {
                    statusString = value;
                    //Status = (ServiceStatus)Enum.Parse(typeof(ServiceStatus), value, false);
                }
                catch
                {
                    Status = ServiceStatus.OnDemandOrUnknown;
                }
            }
        }

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
