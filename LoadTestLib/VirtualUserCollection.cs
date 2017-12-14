using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading;
using Spring.Collections;

namespace LoadTestLib
{
    public class ConnThreadStart : IDisposable
    {
        public Int32 virtualUser = 0;
        public Int32 connectionIndex = 0;

        public override string ToString()
        {
            return virtualUser.ToString() + ":" + connectionIndex.ToString();
        }

        public void Dispose()
        {
            //netMap = null;
        }
    }

    public class VirtualUserCollection
    {
        private Dictionary<String, Thread> _pool;
        private HashedSet virtualUsers;
        private HashedSet connections;
        private Boolean _running;
        private Boolean _EnableDebug;
        private TestEnvironment environment;
        private Timer _cntTimer;

        public Boolean EnableDebug { get { return _EnableDebug; } set { _EnableDebug = value; } }

        public delegate void ErrorReceived(ResultData result);
        public event ErrorReceived OnErrorReceived;

        public delegate void VUCountReceived(DateTime date, Int32 virtualUsersCount, Int32 connectionsCount);
        public event VUCountReceived OnVUCountReceived;

        public delegate void ResultReceived(DateTime date, ResultData result);
        public event ResultReceived OnResultReceived;

        public delegate void DebugEvent(String index, String text);
        public event DebugEvent OnDebugEvent;
        
        public VirtualUserCollection(TestEnvironment environment)
        {
            this.environment = environment;
            this._pool = new Dictionary<String, Thread>();
            this.virtualUsers = new HashedSet();
            this.connections = new HashedSet();
        }

        public void Start()
        {

            if ((_EnableDebug) && (OnDebugEvent != null))
                OnDebugEvent("0", "Start> Iniciando");

            this._running = true;

            _cntTimer = new Timer(new TimerCallback(CntTmrCallback), null, 5000, 200);

            for (Int32 i = 1; i <= this.environment.VirtualUsers; i++)
                for (Int32 u = 1; u <= (this.environment.Type == ClientType.SBU ? this.environment.SBUConcurrentConnections : 1); u++)
                    JoinPool(i, u);
                        
            //Inicia as theads
            for (Int32 i = 1; i <= this.environment.VirtualUsers; i++)
                for (Int32 u = 1; u <= (this.environment.Type == ClientType.SBU ? this.environment.SBUConcurrentConnections : 1); u++)
                    StartPool(i, u);

            
        }

        private void CntTmrCallback(Object state)
        {
            if (OnVUCountReceived != null)
                OnVUCountReceived(DateTime.Now, virtualUsers.Count, connections.Count);
        }

        public void StartPool(Int32 vuIndex, Int32 sbuIndex)
        {
            String pID = vuIndex.ToString() + "-" + sbuIndex.ToString();

            if ((_EnableDebug) && (OnDebugEvent != null))
                OnDebugEvent("0", "Start> Add pool (" + vuIndex + "," + sbuIndex + ") " + pID);

            this._pool.Add(pID, new Thread(new ParameterizedThreadStart(StartThread)));

            this.virtualUsers.Add(vuIndex);
            this.connections.Add(pID);

            if (this._pool.ContainsKey(pID))
            {

                ConnThreadStart oStart = new ConnThreadStart();
                oStart.virtualUser = vuIndex;
                oStart.connectionIndex = sbuIndex;

                this._pool[pID].Start(oStart);
                Thread.Sleep(50);
            }
        }

        public void JoinPool(Int32 vuIndex, Int32 sbuIndex)
        {

            StopPool(vuIndex, sbuIndex);

            //this._pool[i].Start(oStart);
        }

        public void StopPool(Int32 vuIndex, Int32 sbuIndex)
        {
            String pID = vuIndex.ToString() + "-" + sbuIndex.ToString();
            

//            this.virtualUsers.Add(vuIndex);

            if (this._pool.ContainsKey(pID))
            {
                if ((_EnableDebug) && (OnDebugEvent != null))
                    OnDebugEvent("0", "Start> Remove Pool " + pID);

                try
                {
                    this._pool[pID].Abort();
                }
                catch { }
                this._pool[pID] = null;
                this._pool.Remove(pID);
            }

        }

        public void Stop()
        {
            for (Int32 i = 1; i <= this.environment.VirtualUsers; i++)
                for (Int32 u = 1; u <= (this.environment.Type == ClientType.SBU ? this.environment.SBUConcurrentConnections : 1); i++)
                    StopPool(i, u);

            this._running = false;

        }

        private void StartThread(Object oStart)
        {
            ConnThreadStart start = (ConnThreadStart)oStart;
            String pID = start.virtualUser.ToString() + "-" + start.connectionIndex.ToString();

            if ((_EnableDebug) && (OnDebugEvent != null))
                OnDebugEvent(start.ToString(), "StartThread> " + start.virtualUser + ":" + start.connectionIndex);

            //Uri[] uris = environment.Uris.ToArray();

            while (_running)
            {
                foreach (UriInfo u in environment.Uris)
                {
                    this.connections.Add(pID);
                    ResultData request = Request.GetRequest(u.Target, environment.Proxy, environment.HTTPHeaders);
                    this.connections.Remove(pID);

                    try
                    {
                        Array.Clear(request.Data, 0, request.Data.Length);
                        request.Data = null;
                    }
                    catch { }

                    if (OnResultReceived != null)
                        OnResultReceived(DateTime.Now, request);

                    Thread.Sleep(100);
                }

                Thread.Sleep(500);
            }

            //uris = null;

        }
    }
}
