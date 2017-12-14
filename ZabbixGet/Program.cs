using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Data;
using System.Threading;
using SafeTrend.Data;
using LoadTestLib;
using LoadTestLib.ZabbixGet;

namespace ZabbixGet
{


    class Program
    {
        static TestEnvironment env;
        static void Main(string[] args)
        {

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            if (args.Length == 0)
            {
                Console.WriteLine("Lista de argumentos está vazia");
                System.Threading.Thread.Sleep(3000);
                return;
            }

            FileInfo configFile = null;
            try
            {
                configFile = new FileInfo(args[0]);
            }
            catch
            {
                Console.WriteLine("O argumento 0 não é um caminho de arquivo válido");
                System.Threading.Thread.Sleep(3000);
                return;
            }

            if ((configFile == null) || (!configFile.Exists))
            {
                Console.WriteLine("Arquivo de configuração inexistente");
                System.Threading.Thread.Sleep(3000);
                return;
            }

            env = new TestEnvironment();
            try
            {
                env.LoadConfig(configFile.FullName);

                try
                {
                    //configFile.Delete();
                }
                catch { }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Falha ao carregar o arquivo de configuração");
                System.Threading.Thread.Sleep(3000);
                return;
            }

            //Testa conexão com banco de dados
            try
            {
                LoadTestDatabase mydb = new LoadTestDatabase(env.ConnectionString);

                mydb.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Falha ao conectar ao banco de dados: " + ex.Message);
                System.Threading.Thread.Sleep(3000);
                return;
            }


            Console.WriteLine("Starting zabbix monitors...");

            MonitorStarter connectionStarter = new MonitorStarter();
            connectionStarter.SetStartupConfig(env, null);

            connectionStarter.OnDebugEvent += new MonitorStarter.DebugEvent(connectionStarter_OnDebugEvent);
            connectionStarter.OnBulkEvent += new MonitorStarter.BulkEvent(connectionStarter_OnBulkEvent);

            connectionStarter.StartMonitor();

            /*
            if (env.ZabbixMonitors != null)
            {
                foreach (ZabbixConfig c in env.ZabbixMonitors)
                {
                    Thread mon = new Thread(new ParameterizedThreadStart(startThread));
                    mon.Start(c);

                }
            }*/

            /*
            Zabbix zbx = new Zabbix("172.24.0.1");
            //String tst = zbx.GetItem("net.if.discovery");
            String tst = zbx.GetItem("net.if.in[eth0]");
            System.Threading.Thread.Sleep(1000);
            String tst2 = zbx.GetItem("net.if.in[eth0]");

            Console.WriteLine(Int64.Parse(tst2) - Int64.Parse(tst));

            Console.WriteLine(tst);*/

            while(true)
                Console.ReadLine();
        }

        static void connectionStarter_OnBulkEvent(DataTable data, string tableName)
        {

            Console.Write(",");

            for (Int32 t = 1; t <= 3; t++)
            {
                LoadTestDatabase mydb = null;
                try
                {
                    mydb = new LoadTestDatabase(env.ConnectionString);

                    mydb.BulkCopy(data, tableName);

                    data.Dispose();

                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ZabbixMonitor.OnBulkEvent: Falha ao processar os dados " + ex.Message);
                }
                finally
                {
                    if (mydb != null) mydb.Dispose();
                    mydb = null;
                }
            }
        }

        static void connectionStarter_OnDebugEvent(string index, string text)
        {
            //SetStatus(LogType.Debug, text, index);
            //Console.WriteLine(text);

            //Console.WriteLine(index + " ==> " + text);
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }

    }
}
