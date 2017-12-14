using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using SafeTrend.Data;

namespace LoadTest
{
    public class ZabbixConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("zabbixEndpoints")]
        [ConfigurationCollection(typeof(ZabbixConfigCollection), AddItemName = "add")]
        public ZabbixConfigCollection ZabbixConfigElements { get { return (ZabbixConfigCollection)base["zabbixEndpoints"]; } }
    }

    public class ZabbixConfigCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ZabbixConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ZabbixConfigElement)element).Name;
        }
    }

    public class ZabbixConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true)]
        public string Name
        {
            get { return (string)this["name"];}
            set { this["name"] = value;}
        }

        [ConfigurationProperty("host", IsRequired = true)]
        public string Host
        {
            get { return (string)this["host"]; }
            set { this["host"] = value; }
        }

        [ConfigurationProperty("port", IsRequired = false, DefaultValue = 10050)]
        public Int32 Port
        {
            get { return (Int32)this["port"]; }
            set { this["port"] = value; }
        }

        public ZabbixConfig ToZabbixConfig()
        {
            return new ZabbixConfig(this.Name, this.Host, this.Port);
        }
    }

}
