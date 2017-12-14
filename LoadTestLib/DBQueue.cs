using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadTestLib
{
    public class QueueItem : IDisposable
    {
        public VUQueueItem VUCount;
        public WebResultQueueItem Result;
        public TextQueueItem Debug;
        public ZabbixQueueItem Zabbix;

        public QueueItem(DateTime date, Int32 virtualUsersCount, Int32 connectionsCount)
            : this(new VUQueueItem(date, virtualUsersCount, connectionsCount)) { }

        public QueueItem(DateTime date, ResultData result)
            : this(new WebResultQueueItem(date, result)) { }

        public QueueItem(String conn_type, String index, String debug_text)
            : this(new TextQueueItem("{" + conn_type + "} [" + index + "] " + debug_text)) { }

        public QueueItem(DateTime date, String host, String key, Int64 totalValue, Int64 value)
            : this(new ZabbixQueueItem(date, host, key, totalValue, value)) { }

        public QueueItem(VUQueueItem VUCount)
        {
            this.VUCount = VUCount;
            this.Result = null;
            this.Debug = null;
            this.Zabbix = null;
        }

        public QueueItem(WebResultQueueItem Result)
        {
            this.Result = Result;
            this.VUCount = null;
            this.Debug = null;
            this.Zabbix = null;
        }

        public QueueItem(TextQueueItem Debug)
        {
            this.VUCount = null;
            this.Result = null;
            this.Debug = Debug;
            this.Zabbix = null;
        }

        public QueueItem(ZabbixQueueItem Zabbix)
        {
            this.VUCount = null;
            this.Result = null;
            this.Debug = null;
            this.Zabbix = Zabbix;
        }

        public void Dispose()
        {
            if (VUCount != null) VUCount.Dispose();
            if (Result != null) Result.Dispose();
            if (Debug != null) Debug.Dispose();
            if (Zabbix != null) Zabbix.Dispose();

            VUCount = null;
            Result = null;
            Debug = null;
            Zabbix = null;
        }

    }

    public class DBQueue
    {
        private List<QueueItem> _logItems;

        public Int32 Count { get { return _logItems.Count; } }

        public DBQueue()
        {
            _logItems = new List<QueueItem>();
        }

        public void Clear()
        {
            _logItems.Clear();
        }

        public void Add(TextQueueItem log)
        {
            Add(new QueueItem(log));
        }

        public void Add(ZabbixQueueItem log)
        {
            Add(new QueueItem(log));
        }

        public void Add(VUQueueItem log)
        {
            Add(new QueueItem(log));
        }

        public void Add(WebResultQueueItem log)
        {
            Add(new QueueItem(log));
        }

        public void Add(QueueItem log)
        {
            lock (_logItems)
            {
                _logItems.Add(log);
            }
        }

        public QueueItem nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return null;

                    QueueItem item = null;
                    lock (_logItems)
                    {
                        item = _logItems[0];
                        _logItems.RemoveAt(0);
                    }
                    return item;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
