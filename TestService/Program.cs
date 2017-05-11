using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestService
{
    class Program
    {
        static void Main(string[] args)
        {
            do
            {
                ServiceReference1.OdSyncStatusWSClient client = null;
                try
                {
                    client = new ServiceReference1.OdSyncStatusWSClient("pipeBinding_IOdSyncStatusWS");
                    var results = client.GetStatus();
                    foreach (var result in results)
                    {
                        Console.WriteLine("====================================================");
                        Console.WriteLine("Results at {0}", DateTime.Now);
                        Console.WriteLine("User Name     : {0}", result.UserName);
                        Console.WriteLine("User SID      : {0}", result.UserSID);
                        Console.WriteLine("Local Path    : {0}", result.LocalPath);
                        Console.WriteLine("Service Status: {0}", result.StatusString);
                        Console.WriteLine();

                    }
                }
                finally
                {
                    if (client != null)
                    {
                        if (client.State == System.ServiceModel.CommunicationState.Opened)
                        {
                            client.Close();
                        }
                        else
                        {
                            client.Abort();
                        }
                    }
                }

                Console.Write("Press 'q' to quit or any key to repeat...");
                var key = Console.ReadKey();
                if (key.KeyChar == 'q')
                    break;
            } while (true);
        }
    }
}
