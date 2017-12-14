using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SafeTrend.Data
{
    [Serializable()]
    public class ZabbixConfig
    {
        private string name;
        private string host;
        private Int32 port;

        public string Name { get { return name; } }

        public string Host { get { return host; } }

        public Int32 Port { get { return port; } }

        public ZabbixConfig(string name, string host, Int32 port)
        {
            this.name = name;
            this.host = host;
            this.port = port;
        }

    }
}
