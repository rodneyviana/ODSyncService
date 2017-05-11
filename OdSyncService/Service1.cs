using System;
using System.Diagnostics;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;

namespace OdSyncService
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        ServiceHost host = null;
        protected override void OnStart(string[] args)
        {
            WriteLog.WriteToLog(EventLogEntryType.Information, "Service OdSynService is starting");
            host = new ServiceHost(typeof(OdSyncService.OdSyncStatusWS));
            host.Opening += host_Opening;
            host.Opened += host_Opened;
            host.Closing += host_Closing;
            host.Closed += host_Closed;
            host.Faulted += host_Faulted;
            host.Open();
        }

        void host_Faulted(object sender, EventArgs e)
        {
            WriteLog.WriteToLog(EventLogEntryType.Error, String.Format("WCF service {0} failed. This is unexpected. Please restart service.", host.Description.ServiceType));
            
        }

        void host_Closed(object sender, EventArgs e)
        {
            WriteLog.WriteToLog(EventLogEntryType.Warning, String.Format("WCF service {0} was closed. This is unexpected if service is not stopped", host.Description.ServiceType));
        }

        void host_Closing(object sender, EventArgs e)
        {
            return;
        }

        void host_Opened(object sender, EventArgs e)
        {
            StringBuilder str = new StringBuilder(100);
            str.AppendLine(String.Format("WCF service {0} was opened with endpoint(s) below:", host.Description.ServiceType));
            foreach (var endP in host.Description.Endpoints)
            {
                str.AppendLine(String.Format("{0} ({1})", endP.Address, endP.Binding));
            }

            WriteLog.WriteToLog(EventLogEntryType.Information, str.ToString());
            
        }

        void host_Opening(object sender, EventArgs e)
        {
            return;
        }

        protected override void OnStop()
        {
            WriteLog.WriteToLog(EventLogEntryType.Information, "Service OdSynService is shutting down");
            if (host != null && host.State == CommunicationState.Opened)
            {
                host.Close();
            }
            else
            {

            }

        }
    }
}
