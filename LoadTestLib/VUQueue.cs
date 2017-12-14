using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadTestLib
{
    public class VUQueueItem : IDisposable
    {
        public DateTime date;
        public Int32 virtualUsersCount;
        public Int32 connectionsCount;

        public VUQueueItem(DateTime date, Int32 virtualUsersCount, Int32 connectionsCount)
        {
            this.date = date;
            this.virtualUsersCount = virtualUsersCount;
            this.connectionsCount = connectionsCount;
        }

        public void Dispose()
        {
            
        }
    }

    public class VUQueue
    {
        private List<VUQueueItem> _logItems;

        public Int32 Count { get { return _logItems.Count; } }

        public VUQueue()
        {
            _logItems = new List<VUQueueItem>();
        }

        public void Add(VUQueueItem log)
        {
            lock (_logItems)
            {
                _logItems.Add(log);
            }
        }

        /*
        public void Remove(SMTPLogItem log)
        {
            _logItems.Remove(log);
        }*/

        public VUQueueItem nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return null;

                    VUQueueItem item = null;
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
