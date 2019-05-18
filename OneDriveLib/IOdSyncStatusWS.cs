using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

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
