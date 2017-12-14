using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadTestLib
{
    public class WebResultQueueItem : IDisposable
    {
        public DateTime date;
        public ResultData result;

        public WebResultQueueItem(DateTime date, ResultData result)
        {
            this.date = date;
            this.result = result;
        }

        public void Dispose()
        {
            if (result != null) result.Dispose();
            result = null;
        }
    }

    public class WebResultQueue
    {
        private List<WebResultQueueItem> _logItems;

        public Int32 Count { get { return _logItems.Count; } }

        public WebResultQueue()
        {
            _logItems = new List<WebResultQueueItem>();
        }

        public void Add(WebResultQueueItem log)
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

        public WebResultQueueItem nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return null;

                    WebResultQueueItem item = null;
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
