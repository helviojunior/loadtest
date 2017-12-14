using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadTestLib
{
    [Serializable()]
    public class SQLConfig : IDisposable, ICloneable
    {
        public String Server { get; set; }
        public String Database { get; set; }
        public String Username { get; set; }
        public String Password { get; set; }

        public SQLConfig()
            : this("", "", "", "") { }

        public SQLConfig(String Server, String Database, String Username, String Password)
        {
            this.Server = Server;
            this.Database = Database;
            this.Username = Username;
            this.Password = Password;
        }

        public Object Clone()
        {
            SQLConfig cl = new SQLConfig();
            cl.Server = this.Server;
            cl.Database = this.Database;
            cl.Username = this.Username;
            cl.Password = this.Password;

            return cl;
        }

        public void Dispose()
        {
            this.Server = null;
            this.Database = null;
            this.Username = null;
            this.Password = null;
        }
    }
}
