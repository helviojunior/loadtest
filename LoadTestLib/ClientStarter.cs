using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Data;
using System.Net;
using System.IO;

namespace LoadTestLib
{

    public class ClientStarter : IDisposable
    {
        private Int64 i_pid;
        private Boolean _running;
        private DBQueue[] _queue;
        private Boolean _debug;
        private Int32 _insertIndex1;
        private Int32 _insertIndex2;
        private TestEnvironment environment;
        //private DirectoryInfo logDir;
        private VirtualUserCollection userCollection;
        private String uid;
        private Object state;

        private String SQLExternalID;
        private Boolean writeToTextFile;

        //public DirectoryInfo LogDir { get { return logDir; } set { logDir = value; if (!logDir.Exists) logDir.Create(); } }
        public Boolean WriteToTextFile { get { return writeToTextFile; } set { writeToTextFile = value; } }
        
        public delegate void VUCountReceived(DateTime date, Int32 virtualUsersCount, Int32 connectionsCount, Object state);
        public event VUCountReceived OnVUCountReceived;

        public delegate void ResultReceived(DateTime date, ResultData result, Object state);
        public event ResultReceived OnResultReceived;


        public delegate void DebugEvent(String index, String text);
        public event DebugEvent OnDebugEvent;

        public delegate void BulkEvent(DataTable data, String tableName);
        public event BulkEvent OnBulkEvent;

        public ClientStarter(String SQLExternalID)
            : this()
        {
            this.SQLExternalID = SQLExternalID;
        }

        public ClientStarter()
        {
            i_pid = Process.GetCurrentProcess().Id;
            uid = Guid.NewGuid().ToString();
            //this.LogDir = new DirectoryInfo(Path.Combine(Environment.CurrentDirectory, "logs"));

            this.SQLExternalID = null;
            this.writeToTextFile = true;

        }
        /*
        public ConnectionInfo GetConnectionInfo()
        {
            if (startup != null)
                return new ConnectionInfo(startup.ConnectionType, startup.ConnectionCount);
            else
                return new ConnectionInfo("none", 0);
        }*/

        public void SetStartupConfig(TestEnvironment environment)
        {
            SetStartupConfig(environment, null);
        }

        public void SetStartupConfig(TestEnvironment environment, Object state)
        {
            if (environment.Uris == null)
                throw new Exception("URI list is null");

            if (environment.Uris.Count == 0)
                throw new Exception("URI list is empty");

            this.environment = environment;
            this.state = state;

            Debug.WriteLine(environment.ToString());
        }

        public void StartConnections()
        {
            if (_running)
                throw new Exception("Test is running");

            Thread connSt = new Thread(new ThreadStart(StartConn));
            connSt.Start();

        }

        public void StopConnections()
        {
            //Process.GetCurrentProcess().Kill();

            _running = false;

            if (userCollection != null)
                userCollection.Stop();

            userCollection = null;

        }

        public void Dispose()
        {
            this.StopConnections();

            if (_queue != null)
            {
                for (Int32 i = 0; i < _queue.Length; i++)
                {
                    if (_queue[i] != null)
                        _queue[i].Clear();

                    _queue[i] = null;
                }
            }
            _queue = null;

        }

        //Métodos privados
        private void StartConn()
        {

            try
            {
                if (environment.VirtualUsers < 1)
                    environment.VirtualUsers = 1;

                if (environment.VirtualUsers > 1000)
                {
                    Console.WriteLine("ConnectionCount exede o limite estabelecido de 1000. Valor alterado para este limite");
                    environment.VirtualUsers = 1000;
                }

                _running = true;

                Int32 queueCount = (Int32)environment.VirtualUsers / 10;
                queueCount = 3;
                if (queueCount <= 0)
                    queueCount = 1;

                _queue = new DBQueue[queueCount];

                for (Int32 i = 0; i < queueCount; i++)
                {
                    _queue[i] = new DBQueue();
                }

                for (Int32 i = 0; i < queueCount; i++)
                {
                    //ChangeDB(i);
                    Debug.WriteLine("procQueue.Start(i); " + i);

                    _queue[i] = new DBQueue();
                    Thread procQueue = new Thread(new ParameterizedThreadStart(ProcQueue));
                    procQueue.Start(i);
                }

                _debug = false;
                userCollection = new VirtualUserCollection(environment);
                userCollection.EnableDebug = true;

                userCollection.OnVUCountReceived += new VirtualUserCollection.VUCountReceived(evtOnVUCountReceived);
                userCollection.OnResultReceived += new VirtualUserCollection.ResultReceived(evtOnResultReceived);
                userCollection.OnErrorReceived += new VirtualUserCollection.ErrorReceived(evtOnErrorReceived);
                userCollection.OnDebugEvent += new VirtualUserCollection.DebugEvent(evtOnDebugEvent);
                userCollection.OnErrorReceived += new VirtualUserCollection.ErrorReceived(evtOnErrorReceived);
                userCollection.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                SendText("StartConn Error:", ex.Message);
            }
        }

        private void SendText(String function, String text)
        {
            SendText(function, text, false);
        }
        
        private void SendText(String function, String text, Boolean force)
        {

            String texto = null;
            try
            {
                texto = String.Format("[{0}] [{1}] {2}", i_pid, function, text);
                Debug.WriteLine(texto);

#if DEBUG
                    Console.WriteLine(texto);
#endif
                /*
                if ((_debug) || (force))
                    syslog.SendData(texto, Encoding.UTF8);
                */
            }
            catch { }
            finally
            {
                texto = null;
            }
        }


        private void evtOnDebugEvent(string index, string text)
        {
            if (OnDebugEvent != null)
                OnDebugEvent(index, text);
        }

        private void evtOnVUCountReceived(DateTime date, Int32 virtualUsersCount, Int32 connectionsCount)
        {
            if (!_running)
                return;

            try
            {
                _insertIndex1++;
                if (_insertIndex1 > _queue.Length - 1)
                    _insertIndex1 = 0;

                _queue[_insertIndex1].Add(new VUQueueItem(date, virtualUsersCount, connectionsCount));

            }
            catch (Exception ex)
            {
                SendText("evtOnVUCountReceived", ex.Message);
            }

            if (OnVUCountReceived != null)
                OnVUCountReceived(date, virtualUsersCount, connectionsCount, this.state);

        }

        private void evtOnResultReceived(DateTime date, ResultData result)
        {
            //Debug.WriteLine("OnStatisticReceived>");

            if (!_running)
                return;

            try
            {
                _insertIndex2++;
                if (_insertIndex2 > _queue.Length - 1)
                    _insertIndex2 = 0;

                _queue[_insertIndex2].Add(new WebResultQueueItem(date, result));

            }
            catch (Exception ex)
            {
                SendText("OnStatisticReceived", ex.Message);
            }

#if DEBUG
            if (result.Error || result.Code != 200 || !String.IsNullOrEmpty(result.ErrorMessage))
                SendText("OnStatisticReceived with error? Is it error? " + result.Error + " Result Code " + result.Code, result.ErrorMessage);
#endif

            if (OnResultReceived != null)
                OnResultReceived(date, result, this.state);

        }


        private void evtOnErrorReceived(ResultData result)
        {
            SendText("Connection Error:", result.RequestUri + " " + result.ErrorMessage);
        }

        private void ProcQueue2(Object oQueue)
        {
            TextQueue queue = (TextQueue)oQueue;

            FileInfo lFile = new FileInfo(queue.FileName);

            if ((queue.Header != null) && (queue.Header != ""))
                WriteFile(lFile, queue.Header);

            while (_running)
            {
                try
                {
                    TextQueueItem queueItem = null;

                    while ((queueItem = queue.nextItem) != null)
                    {

                        try
                        {
                            WriteFile(lFile, queueItem.text);
                        }
                        catch (Exception ex)
                        {
                            SendText("ProcQueue2", "Error 2: " + ex.Message + ex.StackTrace, true);
                        }

                        if (!_running)
                            break;

                    }//While

                }
                catch (Exception ex)
                {
                    SendText("ProcQueue2", "Error 0: " + ex.Message + ex.StackTrace, true);
                }

                Thread.Sleep(300);
            }
        }

        private void ProcQueue(Object oIndex)
        {
            Int32 index = (Int32)oIndex;

            SendText("ProcQueue " + index, "Start, _running = " + _running);
            Debug.WriteLine("ProcQueue " + index + "Start, _running = " + _running);

            String sIndex = String.Format("{0:00000}", index);

            DataTable vuCountTable = null;
            DataTable webResultTable = null;

            if (OnBulkEvent != null)
            {
                vuCountTable = new DataTable();
                vuCountTable.Columns.Add(new DataColumn("date", typeof(DateTime)));
                vuCountTable.Columns.Add(new DataColumn("dateg", typeof(DateTime)));
                vuCountTable.Columns.Add(new DataColumn("pID", typeof(Int64)));
                vuCountTable.Columns.Add(new DataColumn("testID", typeof(String)));
                vuCountTable.Columns.Add(new DataColumn("virtualUsers", typeof(Int64)));
                vuCountTable.Columns.Add(new DataColumn("connections", typeof(Int64)));

                webResultTable = new DataTable();
                webResultTable.Columns.Add(new DataColumn("date", typeof(DateTime)));
                webResultTable.Columns.Add(new DataColumn("dateg", typeof(DateTime)));
                webResultTable.Columns.Add(new DataColumn("pID", typeof(Int64)));
                webResultTable.Columns.Add(new DataColumn("testID", typeof(String)));
                webResultTable.Columns.Add(new DataColumn("uri", typeof(String)));
                webResultTable.Columns.Add(new DataColumn("statusCode", typeof(Int32)));
                webResultTable.Columns.Add(new DataColumn("contentType", typeof(String)));
                webResultTable.Columns.Add(new DataColumn("bytesReceived", typeof(Int64)));
                webResultTable.Columns.Add(new DataColumn("time", typeof(Double)));
                webResultTable.Columns.Add(new DataColumn("errorMessage", typeof(String)));
                
            }

            Int32 regCount = 0;

            while (_running)
            {
                try
                {
                    QueueItem queueItem = null;

                    while ((queueItem = _queue[index].nextItem) != null)
                    {

                        regCount++;

                        try
                        {
                            //Insere os registros nas tebelas locais temporárias
                            if (queueItem.VUCount != null)
                            {

                                if (vuCountTable != null)
                                    vuCountTable.Rows.Add(new Object[] { 
                                        queueItem.VUCount.date, 
                                        DateGroup(queueItem.VUCount.date), 
                                        i_pid,
                                        environment.TestName,
                                        queueItem.VUCount.virtualUsersCount, 
                                        queueItem.VUCount.connectionsCount
                                    });


                            }//if (queueItem.VUCount != null)

                            if (queueItem.Result != null)
                            {

                                if (webResultTable != null)
                                    webResultTable.Rows.Add(new Object[] { 
                                        queueItem.Result.date, 
                                        DateGroup(queueItem.Result.date), 
                                        i_pid,
                                        environment.TestName,
                                        queueItem.Result.result.RequestUri, 
                                        queueItem.Result.result.Code,
                                        queueItem.Result.result.ContentType,
                                        queueItem.Result.result.DataLength, 
                                        queueItem.Result.result.Time.TotalMilliseconds,
                                        (queueItem.Result.result.ErrorMessage != null ? queueItem.Result.result.ErrorMessage : "")
                                    });

                            }//if (queueItem.Result != null)

                            queueItem.Dispose();
                        }
                        catch (Exception ex)
                        {
                            SendText("ProcQueue", "[" + index + "] Error 2: " + ex.Message + ex.StackTrace, true);
                        }

                        if (!_running)
                            break;

                        //Grava no banco a cada 500 ciclos do while
                        if (regCount > 100)
                        {
                            regCount = 0;
                            break; //Sai do while p/ gravar no db
                        }


                    }//While


                    //Quando sai do while verifica se há registros p/ gerar evento de bulk
                    if (OnBulkEvent != null)
                    {

                        Int32 itensCount = vuCountTable.Rows.Count + webResultTable.Rows.Count;
                        if (((itensCount > 0) && (_running)) || (itensCount >= 100))
                        {
                            try
                            {

                                if (OnBulkEvent != null)
                                {

                                    if (vuCountTable.Rows.Count > 0)
                                        OnBulkEvent(vuCountTable, "VU");

                                    if (webResultTable.Rows.Count > 0)
                                        OnBulkEvent(webResultTable, "WebResult");
                                }

                                vuCountTable.Rows.Clear();
                                webResultTable.Rows.Clear();
                            }
                            catch (Exception ex)
                            {
                                SendText("ProcQueue", "[" + index + "] Error 4: " + ex.Message + ex.StackTrace, true);
                            }
                        }
                    }


                }
                catch (Exception ex)
                {
                    SendText("ProcQueue", "[" + index + "] Error 0: " + ex.Message + ex.StackTrace, true);
                }


                Thread.Sleep(300);
            }
            SendText("ProcQueue", "[" + index + "] Exit: ", true);
            Debug.WriteLine("ProcQueue" + "[" + index + "] Exit: ");
        }

        private void WriteFile(FileInfo file, String text)
        {

            if (!this.writeToTextFile)
                return;

            if (!file.Directory.Exists)
                file.Directory.Create();

            try
            {
                BinaryWriter w = new BinaryWriter(file.Open(FileMode.Append, FileAccess.Write));
                w.Write(Encoding.UTF8.GetBytes(text + Environment.NewLine));
                w.BaseStream.Dispose();
                w.Close();
                w = null;
            }
            catch { }
        }


        public static DateTime DateGroup(DateTime date)
        {
            DateTime newDate = date;
            Int32 year = date.Year;
            Int32 day = date.Day;
            Int32 month = date.Month;
            Int32 hour = date.Hour;
            Int32 minute = date.Minute;
            Int32 second = 0;

            if (date.Second > 50)
            {
                second = 50;
            }
            else if (date.Second > 40)
            {
                second = 40;
            }
            else if (date.Second > 30)
            {
                second = 30;
            }
            else if (date.Second > 20)
            {
                second = 20;
            }
            else if (date.Second > 10)
            {
                second = 10;
            }
            else
            {
                second = 0;
            }


            return new DateTime(year, month, day, hour, minute, second);
        }

    }

}
