using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Data;
using System.Net;
using System.IO;
using SafeTrend.Data;
using SafeTrend.Json;
using LoadTestLib.ZabbixGet;

namespace LoadTestLib
{


    public class MonitorStarter : IDisposable
    {
        protected class MonitorTheadStarter
        {
            public ZabbixConfig config { get; set; }
            public String key { get; set; }
            public Int32 index { get; set; }

            public MonitorTheadStarter(ZabbixConfig config, String key, Int32 index)
            {
                this.config = config;
                this.key = key;
                this.index = index;
            }
        }

        [Serializable]
        public class IfRet
        {
            public List<Dictionary<String, String>> data;
        }

        private Int64 i_pid;
        private Boolean _running;
        private DBQueue[] _queue;
        private TestEnvironment environment;

        private String uid;
        private Object state;

        private String SQLExternalID;
        private Boolean writeToTextFile;

        //public DirectoryInfo LogDir { get { return logDir; } set { logDir = value; if (!logDir.Exists) logDir.Create(); } }
        public Boolean WriteToTextFile { get { return writeToTextFile; } set { writeToTextFile = value; } }

        public delegate void DebugEvent(String index, String text);
        public event DebugEvent OnDebugEvent;

        public delegate void BulkEvent(DataTable data, String tableName);
        public event BulkEvent OnBulkEvent;

        public MonitorStarter(String SQLExternalID)
            : this()
        {
            this.SQLExternalID = SQLExternalID;
        }

        public MonitorStarter()
        {
            i_pid = Process.GetCurrentProcess().Id;
            uid = Guid.NewGuid().ToString();
            
            this.SQLExternalID = null;
            this.writeToTextFile = true;

        }
        
        public void SetStartupConfig(TestEnvironment environment)
        {
            SetStartupConfig(environment, null);
        }

        public void SetStartupConfig(TestEnvironment environment, Object state)
        {
            if (environment.ZabbixMonitors == null)
                throw new Exception("Zabbix monitors list is null");

            if (environment.ZabbixMonitors.Count == 0)
                throw new Exception("URI list is empty");

            this.environment = environment;
            this.state = state;

        }

        public void StartMonitor()
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
                _running = true;

                Int32 queueCount = (Int32)environment.ZabbixMonitors.Count;
                queueCount = 3;
                if (queueCount <= 0)
                    queueCount = 1;

                _queue = new DBQueue[queueCount];

                //Inicia as theads de monitoramento
                Int32 i = 0;
                foreach(ZabbixConfig cfg in environment.ZabbixMonitors){

                    //ChangeDB(i);
                    Debug.WriteLine("procQueue.Start(i); " + i);

                    _queue[i] = new DBQueue();
                    Thread procQueue = new Thread(new ParameterizedThreadStart(ProcQueue));
                    procQueue.Start(i);

                    new Thread(new ParameterizedThreadStart(CounterProc)).Start(new MonitorTheadStarter(cfg, "system.cpu.util[,,avg1]", i));
                    new Thread(new ParameterizedThreadStart(CounterProc)).Start(new MonitorTheadStarter(cfg, "system.cpu.load[percpu,avg1]", i));
                    new Thread(new ParameterizedThreadStart(CounterMem)).Start(new MonitorTheadStarter(cfg, "", i));

                    //Busca as interfaces de rede
                    using (Zabbix zbx = new Zabbix(cfg.Host, cfg.Port))
                    {
                        String netIfs = zbx.GetItem("net.if.discovery");
                        IfRet interfaces = JSON.Deserialize2<IfRet>(netIfs);

                        if ((interfaces != null) && (interfaces.data != null))
                            foreach (Dictionary<String, String> dic in interfaces.data)
                                if (dic != null)
                                    foreach (String val in dic.Values)
                                        if (!String.IsNullOrWhiteSpace(val) && val.ToLower() != "lo")
                                        {
                                            new Thread(new ParameterizedThreadStart(CounterProcNet)).Start(new MonitorTheadStarter(cfg, "net.if.in[" + val + "]", i));
                                            new Thread(new ParameterizedThreadStart(CounterProcNet)).Start(new MonitorTheadStarter(cfg, "net.if.out[" + val + "]", i));
                                        }

                    }


                    i++;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + ex.StackTrace);
                SendText("StartConn Error:", ex.Message);
            }
        }

        private void CounterProc(Object st)
        {
            MonitorTheadStarter info = (MonitorTheadStarter)st;

            Zabbix zbx = new Zabbix(info.config.Host, info.config.Port);
            //String tmp = zbx.GetItem(info.key);

            while (_running)
            {

                switch (info.key.ToLower())
                {
                    case "system.cpu.util[,,avg1]":
                    case "system.cpu.load[percpu,avg1]":
                        try
                        {
                            String tmp3 = zbx.GetItem(info.key).Replace(".",",");
                            double tmp = double.Parse(tmp3) * 100F;
                            _queue[info.index].Add(new ZabbixQueueItem(DateTime.Now, info.config.Host, info.key, 100, (Int64)tmp));
                        }
                        catch { }
                        break;
                        
                    default:
                        try
                        {
                            Int64 tmp = Int64.Parse(zbx.GetItem(info.key));
                            _queue[info.index].Add(new ZabbixQueueItem(DateTime.Now, info.config.Host, info.key, 0, tmp));
                        }
                        catch { }
                        break;

                }

                Thread.Sleep(10000);
            }
        }


        private void CounterMem(Object st)
        {
            MonitorTheadStarter info = (MonitorTheadStarter)st;

            Zabbix zbx = new Zabbix(info.config.Host, info.config.Port);
            //String tmp = zbx.GetItem(info.key);

            //new Thread(new ParameterizedThreadStart(CounterMem)).Start(new MonitorTheadStarter(cfg, "vm.memory.size[total]", i));
            //new Thread(new ParameterizedThreadStart(CounterMem)).Start(new MonitorTheadStarter(cfg, "vm.memory.size[available]", i));

            while (_running)
            {


                try
                {
                    double total = double.Parse(zbx.GetItem("vm.memory.size[total]"));
                    double available = double.Parse(zbx.GetItem("vm.memory.size[available]"));
                    double used = total - available;

                    _queue[info.index].Add(new ZabbixQueueItem(DateTime.Now, info.config.Host, "vm.memory", (Int64)total,(Int64)used));

                    
                }
                catch { }


                Thread.Sleep(10000);
            }
        }


        private void CounterProcNet(Object st)
        {
            MonitorTheadStarter info = (MonitorTheadStarter)st;

            Zabbix zbx = new Zabbix(info.config.Host, info.config.Port);
            //String tmp = zbx.GetItem(info.key);

            ZabbixQueueItem last = new ZabbixQueueItem(DateTime.Now, info.config.Host, info.key, 0, Int64.Parse(zbx.GetItem(info.key)));

            while (_running)
            {

                switch (info.key.ToLower())
                {
                    default:

                        ZabbixQueueItem actual = new ZabbixQueueItem(DateTime.Now, info.config.Host, info.key, 0, Int64.Parse(zbx.GetItem(info.key)));

                        try
                        {
                            double lap = ((TimeSpan)(actual.date - last.date)).TotalSeconds;
                            double lap2 = double.Parse(actual.value.ToString()) - double.Parse(last.value.ToString());

                            double value = (lap2 / lap);

                            if (double.IsNaN(value) || double.IsInfinity(value))
                                value = 0;

                            _queue[info.index].Add(new ZabbixQueueItem(actual.date, actual.host, info.key, 0, ((Int64)value)));

                            last = actual;
                        }
                        catch { }

                        break;

                }

                Thread.Sleep(10000);
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

        private void ProcQueue(Object oIndex)
        {
            Int32 index = (Int32)oIndex;

            SendText("ProcQueue " + index, "Start, _running = " + _running);
            Debug.WriteLine("ProcQueue " + index + "Start, _running = " + _running);

            String sIndex = String.Format("{0:00000}", index);

            DataTable zabbixTable = null;

            if (OnBulkEvent != null)
            {
                zabbixTable = new DataTable();
                zabbixTable.Columns.Add(new DataColumn("date", typeof(DateTime)));
                zabbixTable.Columns.Add(new DataColumn("dateg", typeof(DateTime)));
                zabbixTable.Columns.Add(new DataColumn("pID", typeof(Int64)));
                zabbixTable.Columns.Add(new DataColumn("testID", typeof(String)));
                zabbixTable.Columns.Add(new DataColumn("host", typeof(String)));
                zabbixTable.Columns.Add(new DataColumn("key", typeof(String)));
                zabbixTable.Columns.Add(new DataColumn("selector", typeof(String)));
                zabbixTable.Columns.Add(new DataColumn("total_value", typeof(Int64)));
                zabbixTable.Columns.Add(new DataColumn("value", typeof(Int64)));

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
                            if (queueItem.Zabbix != null)
                            {

                                if (zabbixTable != null)
                                    zabbixTable.Rows.Add(new Object[] { 
                                        queueItem.Zabbix.date, 
                                        DateGroup(queueItem.Zabbix.date), 
                                        i_pid,
                                        environment.TestName,
                                        queueItem.Zabbix.host, 
                                        queueItem.Zabbix.key,
                                        queueItem.Zabbix.selector,
                                        queueItem.Zabbix.totalValue,
                                        queueItem.Zabbix.value
                                    });


                            }//if (queueItem.Zabbix != null)

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

                        Int32 itensCount = zabbixTable.Rows.Count;
                        if (((itensCount > 0) && (_running)) || (itensCount >= 100))
                        {
                            try
                            {

                                if (OnBulkEvent != null)
                                {

                                    if (zabbixTable.Rows.Count > 0)
                                        OnBulkEvent(zabbixTable, "ZabbixMonitor");

                                }

                                zabbixTable.Rows.Clear();
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
