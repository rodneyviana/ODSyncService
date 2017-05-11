using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using System.Reflection;

namespace OdSyncService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static int Main(string[] args)
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new Service1()
            };
            if (System.Environment.UserInteractive)
            {
                if (args.Length != 1 || args[0] == "?" || (args[0].Replace("-","/")+"   ").Substring(0, 2) == "/h")
                {
                    Console.WriteLine("OneDrive Sync Status Service");
                    Console.WriteLine("============================");
                    Console.WriteLine("Syntax: OdSyncService.exe [/i] [/u]");
                    Console.WriteLine("Where:");
                    Console.WriteLine("\t/i install the service");
                    Console.WriteLine("\t/u uninstall the service");
                    return 0; // ok

                }

                // we only care about the first two characters
                string arg = args[0].Replace("-","/").ToLowerInvariant().Substring(0, 2);

                string PathToSelf = Assembly.GetExecutingAssembly().Location;


                switch (arg)
                {
                    case "/i":  // install
                        try
                        {
                            ManagedInstallerClass.InstallHelper(
                                new string[] { PathToSelf });
                            WriteLog.WriteInformationEvent("Service installed successfully");

                            return 0;
                        }
                        catch (Exception ex)
                        {
                            WriteLog.WriteErrorEvent(String.Format("Installation failed: {0} {1}", ex.GetType().ToString(), ex.Message));
                            return -1;
                        }
                        break;
                    case "/u":  // uninstall
                        try
                        {
                            ManagedInstallerClass.InstallHelper(
                                new string[] { "/u", PathToSelf });
                            WriteLog.WriteInformationEvent("Service uninstalled successfully");
                            return 0;
                        }
                        catch (Exception ex)
                        {
                            WriteLog.WriteErrorEvent(String.Format("Uninstallation failed: {0} {1}", ex.GetType().ToString(), ex.Message));
                            return -1;
                        }
                        break;
                    default:  // unknown option
                        Console.WriteLine("Invalid option: {0}", args[0]);
                        Console.WriteLine(string.Empty);

                        return -1;
                }
            }

            ServiceBase.Run(ServicesToRun);
            return 0;
        }
    }
}
