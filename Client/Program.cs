using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using LoadTestLib;

namespace Client
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
            catch(Exception ex) {
                Console.WriteLine("Falha ao conectar ao banco de dados: " + ex.Message);
                System.Threading.Thread.Sleep(3000);
                return;
            }


            Console.WriteLine("Starting with " + env.VirtualUsers + " usuários virtuais...");


            ClientStarter connectionStarter = new ClientStarter();
            connectionStarter.SetStartupConfig(env, null);
            //connectionStarter.LogDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "logs\\" + env.TestName + "\\conn_logs"));
            connectionStarter.OnVUCountReceived += new ClientStarter.VUCountReceived(connectionStarter_OnVUCountReceived);
            connectionStarter.OnResultReceived += new ClientStarter.ResultReceived(connectionStarter_OnResultReceived);
            connectionStarter.OnDebugEvent += new ClientStarter.DebugEvent(connectionStarter_OnDebugEvent);
            connectionStarter.OnBulkEvent += new ClientStarter.BulkEvent(connectionStarter_OnBulkEvent);
            
            connectionStarter.StartConnections();


            while (true)
                Console.ReadLine();

        }


        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }

        static void connectionStarter_OnVUCountReceived(DateTime date, int virtualUsersCount, int connectionsCount, object state)
        {
            //Console.WriteLine(virtualUsersCount + " -- " + connectionsCount);
        }

        static void connectionStarter_OnResultReceived(DateTime date, ResultData result, object state)
        {
            Console.Write(".");
        }

        static void connectionStarter_OnBulkEvent(DataTable data, string tableName)
        {
            //Tabelas disponíveis
            //VU
            //WebResult

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
                    Console.WriteLine("TestMonitor.OnBulkEvent: Falha ao processar os dados " + ex.Message);
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

    }
}
