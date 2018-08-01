using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Data;
using SafeTrend.Data;
using LoadTestLib;

namespace SNMPTrapReceiver
{
   
    class Program
    {
        static TrapQueue _queue;
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

            _queue = new TrapQueue();
            Thread procQueue = new Thread(new ParameterizedThreadStart(ProcQueue));
            procQueue.Start(0);

            int port = 162;
            UdpClient listener;
            IPEndPoint groupEP;
            byte[] packet = new byte[1024];
            
            Console.WriteLine("Initializing SNMP Listener on Port:" + port + "...");

            // try
            // {
            listener = new UdpClient(port);
            groupEP = new IPEndPoint(IPAddress.Any, port);

            while (true)
            {
                packet = listener.Receive(ref groupEP);
                if (packet.Length > 10)
                {
                    try
                    {
                        _queue.Add(DateTime.Now, groupEP, packet);
                        //SNMPTrap trap = new SNMPTrap(DateTime.Now, groupEP, packet);

                    }
                    catch { }
                }
            }
        }


        private static void ProcQueue(Object oIndex)
        {
            Int32 index = (Int32)oIndex;

            LoadTestDatabase db = new LoadTestDatabase(env.ConnectionString);


            while (true)
            {
                try
                {
                    TrapQueueItem queueItem = null;

                    while ((queueItem = _queue.nextItem) != null)
                    {
                        SNMPTrap trap = new SNMPTrap(queueItem.receivedDate, queueItem.receivedEP, queueItem.packet);
                        db.insertMessages(env.TestName, "8. SNMP Trap from " + trap.receivedEP.Address.ToString(), trap.ToString());

#if DEBUG
                        Console.WriteLine(trap.ToString());
#else
                        Console.Write(".");
#endif
                    }
                }
                catch { }

                Thread.Sleep(300);
            }
        }



        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            UnhandledException.WriteEvent(sender, e, true);
        }

    }

}