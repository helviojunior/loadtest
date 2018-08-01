using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace SNMPTrapReceiver
{

    public class TrapQueueItem
    {
        public DateTime receivedDate;
        public IPEndPoint receivedEP;
        public Byte[] packet;

        public TrapQueueItem(DateTime receivedDate, IPEndPoint receivedEP, Byte[] packet)
        {
            this.receivedDate = receivedDate;
            this.receivedEP = new IPEndPoint(receivedEP.Address, receivedEP.Port);
            this.packet = packet;
        }
    }

    public class TrapQueue
    {
        private List<TrapQueueItem> _logItems;

        public Int32 Count { get { return _logItems.Count; } }

        public TrapQueue()
        {
            _logItems = new List<TrapQueueItem>();
        }

        public void Clear()
        {
            _logItems.Clear();
        }

        public void Add(DateTime receivedDate, IPEndPoint receivedEP, Byte[] packet)
        {
            Add(new TrapQueueItem(receivedDate, receivedEP, packet));
        }

        public void Add(TrapQueueItem log)
        {
            lock (_logItems)
            {
                _logItems.Add(log);
            }
        }

        public TrapQueueItem nextItem
        {
            get
            {
                try
                {
                    if (_logItems.Count == 0)
                        return null;

                    TrapQueueItem item = null;
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
