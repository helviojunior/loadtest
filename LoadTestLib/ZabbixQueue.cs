using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadTestLib
{
    public class ZabbixQueueItem : IDisposable
    {
        public DateTime date;
        public String host;
        public String key;
        public String selector;
        public Int64 value;
        public Int64 totalValue;

        public ZabbixQueueItem(DateTime date, String host, String key, Int64 totalValue, Int64 value)
        {
            this.date = date;
            this.host = host;
            this.key = key;

            String[] part = this.key.Split("[".ToCharArray(), 2);
            this.key = part[0];

            if (part.Length == 2)
                this.selector = part[1].Trim("] ".ToCharArray());

            //this.selector = selector;
            this.totalValue = totalValue;
            this.value = value;
        }

        public void Dispose()
        {
            host = null;
            key = null;
            selector = null;
        }
    }

    public class ZabbixQueueNetworkItem : IDisposable
    {
        public DateTime date;
        public String host;
        public String networkInterface;
        public Int64 inValue;
        public Int64 outValue;

        public ZabbixQueueNetworkItem(DateTime date, String host, String networkInterface, Int64 inValue, Int64 outValue)
        {
            this.date = date;
            this.host = host;
            this.networkInterface = networkInterface;
            this.inValue = inValue;
            this.outValue = outValue;

        }

        public void Dispose()
        {
            host = null;
            networkInterface = null;
        }
    }


    public class ZabbixQueue
    {
        private List<ZabbixQueueItem> _logItems;

        public Int32 Count { get { return _logItems.Count; } }

        public ZabbixQueue()
        {
            this._logItems = new List<ZabbixQueueItem>();
        }

        public void Add(ZabbixQueueItem log)
        {
            lock (_logItems)
            {
                _logItems.Add(log);
            }
        }

        public ZabbixQueueItem nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return null;

                    ZabbixQueueItem item = null;
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
