﻿using System;
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
using System.Diagnostics;
using System.Security.Principal;

namespace LoadTest
{

    class Program
    {
        static private Boolean _running = false;
        static private Timer _tmpEnd;
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();

            WindowsPrincipal pricipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            bool hasAdministrativeRight = pricipal.IsInRole(WindowsBuiltInRole.Administrator);

            if (!hasAdministrativeRight)
            {
                string parameter = string.Concat(args);
                RunElevated(asm.Location, parameter);
                Process.GetCurrentProcess().Kill();
            }

            Console.WriteLine("[+] Checking initial configuration...");

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

            Console.WriteLine("[+] Checking zabbix agents...");


            Console.WriteLine("[+] Checking date and time from NTP servers...");

            try
            {

                DateTime ntpdate = NTP.GetNetworkUTCTime();
                Console.WriteLine("          NTP UTC data: " + ntpdate.ToString(Thread.CurrentThread.CurrentCulture));
                TimeSpan ts = ntpdate - DateTime.UtcNow;
                if (Math.Abs(ts.TotalSeconds) > 60)
                {
                    Console.WriteLine("          Updating local time");
                    Console.WriteLine("          Old time: " + DateTime.Now.ToString(Thread.CurrentThread.CurrentCulture));
                    NTP.SetSystemTime(ntpdate);
                    Console.WriteLine("          New time: " + DateTime.Now.ToString(Thread.CurrentThread.CurrentCulture));
                }
                else
                {
                    Console.WriteLine("          Local time is up to date");

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("[!] Error updating local time: " + ex.Message);
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
                        Console.Write("[*] Zabbix agent on " + zbxHost.Host + ":" + zbxHost.Port);
                        using (Zabbix zbx = new Zabbix(zbxHost.Host, zbxHost.Port))
                        {
                            String tst = zbx.GetItem("system.hostname");
                        }

                        zbxConfig.Add(new ZabbixConfig(zbxHost.Name, zbxHost.Host, zbxHost.Port));
                        Console.WriteLine("\t\tOK");
                    }
                    catch {
                        Console.WriteLine("\t\tError");
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
            if (!String.IsNullOrEmpty((string)ConfigurationManager.AppSettings["proxy"]))
            {
                Uri tProxy = null;
                try
                {
                    tProxy = new Uri(ConfigurationManager.AppSettings["proxy"]);
                    proxy = new IPEndPoint(IPAddress.Parse(tProxy.Host), tProxy.Port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Proxy not valid. Proxy should be an IP URI. Ex: http://10.10.10.10:3389");
                    return;
                }
            }

            Int32 sleepTime = 0;
            try
            {
                Int32.TryParse((string)ConfigurationManager.AppSettings["sleeptime"], out sleepTime);
            }
            catch { }

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

            //

            Console.WriteLine("[+] Building test environment...");
            builder.Fetch(
                    site,
                    proxy,
                    levels,
                    headers);

            TestEnvironment env = builder.Build(proxy, type, 5);
            env.HTTPHeaders = headers;
            env.ZabbixMonitors = zbxConfig;
            env.SleepTime = sleepTime;

            env.VirtualUsers = connCount;
            env.ConnectionString = new DbConnectionString(ConfigurationManager.ConnectionStrings["LoadTest"]);
            env.Start();

            Console.WriteLine("[+] Starting test...");

            Console.WriteLine("[+] System started (" + (prof != null ? "prof" : "scan") + ")!");
            Console.WriteLine("[+] Total test duration: " + duration + " seconds");
            Console.WriteLine("[!] Press CTRL + C to finish the teste and generate report");

            _running = true;
            _tmpEnd = new Timer(new TimerCallback(tmpEnd), null, duration * 1000, duration * 1000);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(myHandler);

            Console.WriteLine("");

            DateTime dStart = DateTime.Now;
            while (_running)
            {
                System.Threading.Thread.Sleep(500);
                TimeSpan ts = DateTime.Now - dStart;
                Console.Write("\r");
                Console.Write("Test Duration: {0}", ts.ToString(@"hh\:mm\:ss"));
            }
            ClearCurrentConsoleLine();
            Console.WriteLine("");

            env.Stop();
            
            /*
            TestEnvironment env = new TestEnvironment();
            env.LoadConfig("temp.env");*/

            //Gera o relatório
            Console.WriteLine("[+] Building report...");
            env.BuildReports();
            //env.DropDatabase();
        }

        private static bool RunElevated(string fileName, String arguments)
        {
            //MessageBox.Show("Run: " + fileName);
            ProcessStartInfo processInfo = new ProcessStartInfo();
            processInfo.Verb = "runas";
            processInfo.FileName = fileName;
            processInfo.Arguments = arguments;
            try
            {
                Process p = Process.Start(processInfo);
                //p.WaitForExit();
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                //Do nothing. Probably the user canceled the UAC window
            }
            return false;
        }
        protected static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
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
