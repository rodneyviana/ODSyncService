using Microsoft.VisualStudio.TestTools.UnitTesting;
using OdSyncService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OdSyncService.Tests
{
    [TestClass()]
    public class OdSyncStatusWSTests
    {
        [TestMethod()]
        public void GetStatusTest()
        {
            OdSyncStatusWS sync = new OdSyncStatusWS();

            var status = sync.GetStatus(@"D:\Onedrive\");

            Assert.AreNotEqual(status, ServiceStatus.NotInstalled);
        }
    }
}