using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Integration;
using Integration.Configuration;
using Topshelf;

namespace ShopifyFBConsole
{
    class Program
    {
        private static bool Debugger = true;
        private static bool FileLog = true;
        private static bool ConsoleLog = true;
        static void Main(string[] args)
        {
            var cfg = Config.Load();
            Config.Save(cfg);
            if (args == null || args.Length == 0 || args[0] != "console")
            {
                HostFactory.Run(x => //1
                {
                    x.Service<ShopifyUpdate>(s => //2
                    {
                        s.ConstructUsing(name => new ShopifyUpdate()); //3
                        s.WhenStarted(tc => tc.Start()); //4
                        s.WhenStopped(tc => tc.Stop()); //5
                    });
                    x.RunAsLocalSystem(); //6

                    x.SetDescription("Shopify Integration for Fishbowl"); //7
                    x.SetDisplayName("Shopify - " + cfg.Service.InstanceName ?? "DEFAULT"); //8
                    x.SetServiceName("Shopify Fishbowl " + cfg.Service.InstanceName ?? "DEFAULT"); //9
                    x.EnableServiceRecovery(r =>
                    {
                        r.RestartService(0); // Restart on First Crash
                        r.RestartService(0); // On Second Crash
                        r.RestartService(0); // On Subsequent Crashes
                    });
                });
            }
            else
            {
                var mage = new ShopifyUpdate();
                if (args.Length == 1)
                {
                    mage.RunIntegration();
                    Console.WriteLine("Done");
                    Console.ReadKey();
                }
            }
        }

        public class ShopifyUpdate
        {
            private System.Timers.Timer _t { get; set; }
            public void Start()
            {
                var cfg = Config.Load();
                if (_t == null)
                {
                    _t = new System.Timers.Timer(cfg.Service.IntervalMinutes * 1000 * 60);
                    _t.Elapsed += _t_Elapsed;
                    _t.AutoReset = false;
                }
                _t.Start();
            }

            private void _t_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
            {
                RunIntegration();
                _t.Start(); // Start only after the Integration Runs Correctly.
            }

            public void Stop()
            {
                if (_t != null)
                {
                    _t.Stop();
                }
            }

            public void RunIntegration()
            {

                try
                {
                    var cfg = Config.Load();
                    using (var app = new ShopifyIntegration(cfg))
                    {
                        app.OnLog += App_OnLog;
                        app.Run();
                    }
                }
                catch (Exception ex)
                {
                    File.WriteAllText("fatal-exception.txt", ex.ToString());
                    Environment.Exit(1);
                }

            }

            private static void App_OnLog(string msg)
            {
                msg = DateTime.Now.ToString() + " - " + msg;
                Console.WriteLine(msg);
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "app.log", msg + Environment.NewLine);
            }
            private static void ExceptionLog(Exception exception)
            {
                String msg = exception.Message;
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "consoleexception.txt", exception.ToString() + Environment.NewLine);
                Ocf_OnLog(msg);
            }

            private static void Ocf_OnLog(string msg)
            {
                String m = DateTime.Now.ToString() + " - " + msg;
                if (Debugger)
                {
                    Debug.WriteLine(m);
                }
                if (FileLog)
                {
                    File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + "log.txt", m + Environment.NewLine);
                }
                if (ConsoleLog)
                {
                    Console.WriteLine(m);
                }
            }
        }
    }
}