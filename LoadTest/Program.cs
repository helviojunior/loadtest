using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using LoadTestLib;
using LoadTestLib.ZabbixGet;
using System.Net;
using System.Threading;
using System.IO;
using System.Configuration;
using SafeTrend.Data;


namespace LoadTest
{

    class Program
    {
        static private Boolean _running = false;
        static private Timer _tmpEnd;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);


            Int16 levels = 3;
                Int16.TryParse(ConfigurationManager.AppSettings["levels"], out levels);


            Int16 connCount = 30;
                Int16.TryParse(ConfigurationManager.AppSettings["count"], out connCount);

            Int16 duration = 300;
                Int16.TryParse(ConfigurationManager.AppSettings["duration"], out duration);

            if (duration < 180)
                duration = 180;

            if (duration > 3600)
                duration = 3600;

            ClientType type = ClientType.VU;
            try
            {
                switch (ConfigurationManager.AppSettings["type"].ToLower())
                {
                    case "sbu":
                        type = ClientType.SBU;
                        break;

                    default:
                        type = ClientType.VU;
                        break;

                }
            }
            catch { }

            Uri site = null;
            try
            {
                site = new Uri(ConfigurationManager.AppSettings["uri"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("URI not valid");
                return;
            }

            Dictionary<String, String> headers = new Dictionary<string, string>();
            if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["Cookie"]))
                try
                {
                    CookieContainer tmp = new CookieContainer();
                    tmp.SetCookies(site, ConfigurationManager.AppSettings["Cookie"]);

                    headers.Add("Cookie", ConfigurationManager.AppSettings["Cookie"]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error parsing cookie");
                    return;
                }


            try
            {

                if (!String.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["User-Agent"]))
                    headers.Add("User-Agent", ConfigurationManager.AppSettings["User-Agent"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing User-Agent");
                return;
            }

            List<ZabbixConfig> zbxConfig = new List<ZabbixConfig>();
            
            ZabbixConfigSection ZabbixManagers = (ZabbixConfigSection)ConfigurationManager.GetSection("zabbixMonitors");
            if (ZabbixManagers != null)
            {
                foreach (ZabbixConfigElement zbxHost in ZabbixManagers.ZabbixConfigElements)
                {
                    //Realiza teste de conex'ao em cada um dos zabbix listados
                    try
                    {
                        using (Zabbix zbx = new Zabbix(zbxHost.Host, zbxHost.Port))
                        {
                            String tst = zbx.GetItem("system.hostname");
                        }

                        zbxConfig.Add(new ZabbixConfig(zbxHost.Name, zbxHost.Host, zbxHost.Port));
                    }
                    catch {
                        Console.WriteLine("Error Getting information from Zabbix " + zbxHost.Name + " (" + zbxHost.Host + ":" + zbxHost.Port + ")");
                        return;
                    }
                }
            }

            Profile prof = null;
            /*
            if (args.Length > 4)
            {
                try
                {
                    FileInfo profileFile = new FileInfo(args[4]);
                    if (profileFile.Extension == ".prof")
                    {
                        prof = new Profile();
                        prof.LoadProfile(profileFile.FullName);

                        if ((prof.BaseUri == null) || (prof.Uris.Count == 0))
                            prof = null;
                    }
                }
                catch(Exception ex) {
                    prof = null;
                }
            }*/
            
            IPEndPoint proxy = null;// new IPEndPoint(IPAddress.Parse("10.0.10.1"), 80);

            TestBuilder builder = new TestBuilder();
            /*
            if (prof != null)
                builder.Fetch(prof);
            else
                builder.Fetch(
                    site,
                    proxy, 
                    1);
            */

            
            
            builder.Fetch(
                    site,
                    proxy,
                    levels,
                    headers);

            TestEnvironment env = builder.Build(proxy, type, 5);
            env.HTTPHeaders = headers;
            env.ZabbixMonitors = zbxConfig;

            env.VirtualUsers = connCount;
            env.ConnectionString = new DbConnectionString(ConfigurationManager.ConnectionStrings["LoadTest"]);
            env.Start();

            Console.WriteLine("Sistema iniciado (" + (prof != null ? "prof" : "scan") + ")!");
            Console.WriteLine("Duração do teste: " + duration + " segundos");
            Console.WriteLine("Para finalizar o teste pressionte CTRL + C");

            _running = true;
            _tmpEnd = new Timer(new TimerCallback(tmpEnd), null, duration * 1000, duration * 1000);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);

            while (_running)
            {
                System.Threading.Thread.Sleep(500);
            }

            env.Stop();
            
            /*
            TestEnvironment env = new TestEnvironment();
            env.LoadConfig("temp.env");*/

            //Gera o relatório
            Console.WriteLine("Gerando relatório...");
            env.BuildReports();
            //env.DropDatabase();
        }

        protected static void tmpEnd(object sender)
        {
            _running = false;
        }

        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            _running = false;
        }

        private static void Use()
        {
            
        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }

    }
}
