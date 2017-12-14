using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadTestLib
{
    public class TextQueueItem : IDisposable
    {
        public String text;

        public TextQueueItem(String text)
        {
            this.text = text;
        }

        public void Dispose()
        {
            text = null;
        }
    }

    public class TextQueue
    {
        private List<TextQueueItem> _logItems;
        private String _fileName;
        private String _header;

        public Int32 Count { get { return _logItems.Count; } }
        public String FileName { get { return _fileName; } }
        public String Header { get { return _header; } }

        public TextQueue(String fileName, String header)
        {
            this._logItems = new List<TextQueueItem>();
            this._fileName = fileName;
            this._header = header;
        }

        public void Add(TextQueueItem log)
        {
            lock (_logItems)
            {
                _logItems.Add(log);
            }
        }

        public TextQueueItem nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return null;

                    TextQueueItem item = null;
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
