using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using SafeTrend.Data;
using SafeTrend.Data.SqlClient;
using SafeTrend.Data.SQLite;

namespace LoadTestLib
{

    [Serializable()]
    public class LoadTestDatabase : DbBase, IDisposable
    {
        private DbBase baseDB;

        public LoadTestDatabase()
            : this(new DbConnectionString(ConfigurationManager.ConnectionStrings["LoadTest"]))
        {
            
        }

        public LoadTestDatabase(DbConnectionString connectionString)
            : base()
        {

            //Set Data Directory, usado pelo .net para substituir |DataDirectory| no connection string
            System.Reflection.Assembly asm = System.Reflection.Assembly.GetAssembly(this.GetType());
            String dir = Path.Combine(Path.GetDirectoryName(asm.Location), "DB");
            AppDomain.CurrentDomain.SetData("DataDirectory", dir);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            this.baseDB = DbBase.InstanceFromConfig(connectionString);
        }

        public String GetDBConfig(String key)
        {
            DataTable dt = Select("select * from server_config with(nolock) where data_name = '" + key + "'");
            if ((dt == null) || (dt.Rows.Count == 0))
                return "";

            return dt.Rows[0]["data_value"].ToString();
        }


        public DataTable selectVUAndConnections(String testId, DateTime dStart, DateTime dEnd)
        {
            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select dateg, sum(virtualusers) as virtualusers, sum(connections) as connections from (select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, AVG(virtualusers) AS virtualusers, AVG(connections) AS connections " +
                    "from VU " +
                    "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                    "and testID = '" + testId + "' " +
                    "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120), pid) as u  group by u.dateg order by u.dateg";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select dateg, cast(sum(virtualusers) as integer) as virtualusers, cast(sum(connections) as integer) as connections from (select strftime('%Y-%m-%d %H:%M:00', dateg) AS dateg, AVG(virtualusers) AS virtualusers, AVG(connections) AS connections " +
                    "from VU " +
                    "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                    "and testID = '" + testId + "' " +
                    "group by strftime('%Y-%m-%d %H:%M:00', dateg), pid) as u  group by u.dateg order by u.dateg";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }
            return Select(SQL);
        }

        public DataTable selectRequests(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, Count(uri) AS requests, cast((Count(uri)/60) as bigint) AS rps " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select strftime('%Y-%m-%d %H:%M:00', dateg) AS dateg, Count(uri) AS requests, cast((Count(uri)/60) as bigint) AS rps " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "group by strftime('%Y-%m-%d %H:%M:00', dateg) " +
                "order by strftime('%Y-%m-%d %H:%M:00', dateg)";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }
            return Select(SQL);
        }

        public DataTable selectErrors(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, cast(Count(uri) as bigint) AS errors " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' and statusCode <> 200 " +
                "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select strftime('%Y-%m-%d %H:%M:00', dateg) AS dateg, cast(Count(uri) as bigint) AS errors " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' and statusCode <> 200 " +
                "group by strftime('%Y-%m-%d %H:%M:00', dateg) " +
                "order by strftime('%Y-%m-%d %H:%M:00', dateg)";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }
            return Select(SQL);
        }

        public DataTable selectThroughput(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, sum(bytesReceived) AS bytes, cast(((sum(bytesReceived) * 8)/60) as bigint) mbps " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select strftime('%Y-%m-%d %H:%M:00', dateg) AS dateg, sum(bytesReceived) AS bytes, cast(((sum(bytesReceived) * 8)/60) as bigint) mbps " +
                 "from WebResult " +
                 "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                 "and testID = '" + testId + "' " +
                 "group by strftime('%Y-%m-%d %H:%M:00', dateg) " +
                 "order by strftime('%Y-%m-%d %H:%M:00', dateg)";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }
            return Select(SQL);
        }

        public DataTable selectResponseTime(String testId, DateTime dStart, DateTime dEnd, Uri target)
        {

            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, CAST(avg(time) AS BIGINT) AS time " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' ";
                if (target != null)
                    SQL += "and uri = '" + target.AbsoluteUri + "' ";
                SQL += "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select strftime('%Y-%m-%d %H:%M:00', dateg) AS dateg, CAST(avg(time) AS BIGINT) AS time " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' ";
                if (target != null)
                    SQL += "and uri = '" + target.AbsoluteUri + "' ";
                SQL += "group by strftime('%Y-%m-%d %H:%M:00', dateg) " +
                "order by strftime('%Y-%m-%d %H:%M:00', dateg)";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }
            return Select(SQL);
        }

        public DataTable selectContentTypeDistribution(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select  " +
                "	(select cast(COUNT(*) as bigint) from WebResult where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') AS total " +
                "	,cast(COUNT(*) as bigint) AS qty " +
                "	, contentType " +
                "from WebResult " +
                "where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by contentType " +
                "order by contentType";

            return Select(SQL);
        }


        public DataTable selectContentTypeTimeDistribution(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select  " +
                "	(select cast(sum(time) as bigint) as time from (select avg(time) as time, contentType from WebResult where statusCode = 200  and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by contentType) as u) AS total " +
                "	,cast(avg(time) as bigint) AS time " +
                "	, contentType " +
                "from WebResult " +
                "where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by contentType " +
                "order by contentType";

            return Select(SQL);
        }


        public DataTable selectContentTypeBytes(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select  " +
                "	(select cast(sum(bytesReceived) as bigint) from WebResult where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') AS total " +
                "	,cast(sum(bytesReceived) as bigint) AS bytesReceived " +
                "	, contentType " +
                "from WebResult " +
                "where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by contentType " +
                "order by contentType";

            return Select(SQL);
        }



        public DataTable selectTopTime(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select " + (this.baseDB is SqlBase ? "top 25" : "") + " uri, contentType, cast(AVG(time) as bigint) AS time " +
                "from WebResult " +
                "where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by uri, contentType " +
                "order by AVG(time) desc, uri, contentType " + (this.baseDB is SqliteBase ? "LIMIT 25" : "");

            return Select(SQL);
        }


        public DataTable selectTopHit(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select " + (this.baseDB is SqlBase ? "top 25" : "") + "  " +
                "	uri " +
                "	,(select COUNT(*) from WebResult w1 where w1.statusCode = 200 and w1.uri = wr.uri and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' ) AS success " +
                "	,(select COUNT(*) from WebResult w1 where w1.statusCode <> 200 and w1.uri = wr.uri and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' ) AS error " +
                "from WebResult wr " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by uri " +
                "order by Count(*) desc " + (this.baseDB is SqliteBase ? "LIMIT 25" : "");

            return Select(SQL);
        }

        public DataTable selectTopBytes(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select " + (this.baseDB is SqlBase ? "top 25" : "") + "  " +
                "	uri " +
                "	,SUM(bytesReceived) AS bytesReceived " +
                "	,Count(bytesReceived) AS qty " +
                "from WebResult wr " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by uri " +
                "order by SUM(bytesReceived) desc " + (this.baseDB is SqliteBase ? "LIMIT 25" : "");

            return Select(SQL);
        }

        public DataTable selectMessages(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select * " +
                "from [Messages] m " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "order by title, date";

            return Select(SQL);
        }

        public DataTable selectOptimization(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select * " +
                "from [Optimization] m " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "order by date";

            return Select(SQL);
        }

        public DataTable selectNonOptimization(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select * " +
                "from [NonOptimization] m " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "order by date";

            return Select(SQL);
        }

        public DataTable selecGzipOptimization(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select * " +
                "from [GzipOptimization] m " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "order by date";

            return Select(SQL);
        }

        public DataTable selectGeneral(String testId, DateTime dStart, DateTime dEnd)
        {
            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select  " +
                    "isnull((select sum(virtualUsers) from (select MAX(virtualUsers) as virtualUsers, pid from VU where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by pid) as u), 0) AS VU " +
                    ",isnull((select sum(connections) from (select MAX(connections) as connections, pid from VU where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by pid) as u),0) AS connections" +
                    ",isnull((select cast(((sum(bytesReceived) * 8)/DATEDIFF(SECOND, MIN(date), MAX(date))) as bigint) from WebResult  where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS throughput " +
                    ",isnull((select sum(bytesReceived) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS bytesReceived " +
                    ",isnull((select cast(count(*) as bigint) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS requests " +
                    ",isnull((select cast((count(*) / DATEDIFF(SECOND, MIN(date), MAX(date))) as bigint) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS rps";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select  " +
                    "ifnull((select sum(virtualUsers) from (select MAX(virtualUsers) as virtualUsers, pid from VU where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by pid) as u), 0) AS VU " +
                    ",ifnull((select sum(connections) from (select MAX(connections) as connections, pid from VU where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by pid) as u),0) AS connections" +
                    ",ifnull((select cast(((sum(bytesReceived) * 8)/(strftime('%s',MAX(date)) - strftime('%s',MIN(date)))) as bigint) from WebResult  where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS throughput " +
                    ",ifnull((select sum(bytesReceived) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS bytesReceived " +
                    ",ifnull((select cast(count(*) as bigint) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS requests " +
                    ",ifnull((select cast((count(*) / (strftime('%s',MAX(date)) - strftime('%s',MIN(date))) ) as bigint) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "'),0) AS rps";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }

            return Select(SQL);
        }


        public DataTable selectMonitoredZabbix(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select host " +
                "from [ZabbixMonitor] m " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "group by host order by host";

            return Select(SQL);
        }



        public DataTable selectZabbixCPU(String testId, String host, DateTime dStart, DateTime dEnd)
        {

            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, CAST(avg(value) AS BIGINT) AS cpu " +
                "from [ZabbixMonitor] " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' ";
                SQL += "and host = '" + host + "' ";
                SQL += "and [key] = 'system.cpu.util' ";
                SQL += "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select strftime('%Y-%m-%d %H:%M:00', dateg) AS dateg, CAST(avg(value) AS BIGINT) AS cpu " +
                "from [ZabbixMonitor] " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' ";
                SQL += "and host = '" + host + "' ";
                SQL += "and key = 'system.cpu.util' ";
                SQL += "group by strftime('%Y-%m-%d %H:%M:00', dateg) " +
                "order by strftime('%Y-%m-%d %H:%M:00', dateg)";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }
            return Select(SQL);
        }

        public DataTable selectZabbixMemory(String testId, String host, DateTime dStart, DateTime dEnd)
        {

            String SQL = "";

            if (this.baseDB is SqlBase)
            {
                SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, CAST(max(total_value) AS BIGINT) AS total_memory, CAST(avg(value) AS BIGINT) AS memory " +
                "from [ZabbixMonitor] " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' ";
                SQL += "and host = '" + host + "' ";
                SQL += "and [key] = 'vm.memory' ";
                SQL += "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";
            }
            else if (this.baseDB is SqliteBase)
            {
                SQL = "select strftime('%Y-%m-%d %H:%M:00', dateg) AS dateg, CAST(max(total_value) AS BIGINT) AS total_memory, CAST(avg(value) AS BIGINT) AS memory " +
                "from [ZabbixMonitor] " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' ";
                SQL += "and host = '" + host + "' ";
                SQL += "and key = 'vm.memory' ";
                SQL += "group by strftime('%Y-%m-%d %H:%M:00', dateg) " +
                "order by strftime('%Y-%m-%d %H:%M:00', dateg)";
            }
            else
            {
                throw new NotImplementedException(string.Format("The database '{0}' is not supported yet", this.baseDB.GetType()));
            }
            return Select(SQL);
        }



        public void insertMessages(String testId, String title, String text)
        {

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@date", typeof(DateTime)).Value = DateTime.Now;
            par.Add("@pid", typeof(Int64)).Value = System.Diagnostics.Process.GetCurrentProcess().Id;
            par.Add("@testId", typeof(String), testId.Length).Value = testId;
            par.Add("@title", typeof(String), title.Length).Value = title;
            par.Add("@text", typeof(String), text.Length).Value = text;

            ExecuteNonQuery("insert into Messages ([date] ,[pid] ,[testId] ,[title] ,[text]) values (@date ,@pid ,@testId ,@title ,@text)", System.Data.CommandType.Text, par, null);


        }

        public void insertOptimization(String testId, Uri uri, Int64 originalLength, Int64 optimizedLength)
        {

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@date", typeof(DateTime)).Value = DateTime.Now;
            par.Add("@pid", typeof(Int64)).Value = System.Diagnostics.Process.GetCurrentProcess().Id;
            par.Add("@testId", typeof(String), testId.Length).Value = testId;
            par.Add("@uri", typeof(String), uri.AbsoluteUri.Length).Value = uri.AbsoluteUri;
            par.Add("@originalLength", typeof(Int64)).Value = originalLength;
            par.Add("@optimizedLength", typeof(Int64)).Value = optimizedLength;

            ExecuteNonQuery("insert into Optimization ([date] ,[pid] ,[testId] ,[uri] ,[originalLength] ,[optimizedLength]) values (@date ,@pid ,@testId ,@uri ,@originalLength ,@optimizedLength)", System.Data.CommandType.Text, par, null);

        }

        public void insertNonOptimization(String testId, Uri uri, Int64 originalLength, Int64 optimizedLength)
        {

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@date", typeof(DateTime)).Value = DateTime.Now;
            par.Add("@pid", typeof(Int64)).Value = System.Diagnostics.Process.GetCurrentProcess().Id;
            par.Add("@testId", typeof(String), testId.Length).Value = testId;
            par.Add("@uri", typeof(String), uri.AbsoluteUri.Length).Value = uri.AbsoluteUri;
            par.Add("@originalLength", typeof(Int64)).Value = originalLength;
            par.Add("@nonOptimizedLength", typeof(Int64)).Value = optimizedLength;

            ExecuteNonQuery("insert into NonOptimization ([date] ,[pid] ,[testId] ,[uri] ,[originalLength] ,[nonOptimizedLength]) values (@date ,@pid ,@testId ,@uri ,@originalLength ,@nonOptimizedLength)", System.Data.CommandType.Text, par, null);

        }

        public void insertGzipOptimization(String testId, Uri uri, Int64 gzipLength, Int64 contentLength)
        {

            DbParameterCollection par = new DbParameterCollection();
            par.Add("@date", typeof(DateTime)).Value = DateTime.Now;
            par.Add("@pid", typeof(Int64)).Value = System.Diagnostics.Process.GetCurrentProcess().Id;
            par.Add("@testId", typeof(String), testId.Length).Value = testId;
            par.Add("@uri", typeof(String), uri.AbsoluteUri.Length).Value = uri.AbsoluteUri;
            par.Add("@gzipLength", typeof(Int64)).Value = gzipLength;
            par.Add("@contentLength", typeof(Int64)).Value = contentLength;

            ExecuteNonQuery("insert into GzipOptimization ([date] ,[pid] ,[testId] ,[uri] ,[gzipLength] ,[contentLength]) values (@date ,@pid ,@testId ,@uri ,@gzipLength ,@contentLength)", System.Data.CommandType.Text, par, null);

        }

        public override void CreateDatabase(String dbName)
        {
            baseDB.CreateDatabase(dbName);
        }

        public override void DropDatabase(String dbName)
        {
            baseDB.DropDatabase(dbName);
        }


        public override DataTable GetSchema(String tableName)
        {
            return baseDB.GetSchema(tableName);
        }

        public override void BulkCopy(DataTable source, String table, Object transaction)
        {
            baseDB.BulkCopy(source, table, transaction);
        }

        public override T ExecuteScalar<T>(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {
            return baseDB.ExecuteScalar<T>(command, commandType, parameters, transaction);
        }

        public override DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {
            return baseDB.ExecuteDataTable(command, commandType, parameters, transaction);
        }

        public override void ExecuteNonQuery(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {
            baseDB.ExecuteNonQuery(command, commandType, parameters, transaction);
        }

        public override Object BeginTransaction()
        {
            if (baseDB is SqlBase)
                return baseDB.BeginTransaction();
            else
                return null;
        }

        public override void Commit()
        {
            if (baseDB is SqlBase)
                baseDB.Commit();
        }

        public override void Rollback()
        {
            if (baseDB is SqlBase)
                baseDB.Rollback();
        }

        public override void Dispose()
        {
            if (baseDB is SqlBase)
                baseDB.Dispose();
        }
    }
}
