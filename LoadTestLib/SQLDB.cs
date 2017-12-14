/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBManager;
using System.Data;
using System.Data.SqlClient;

namespace LoadTestLib
{
    public class SQLDB : BaseDB
    {
        private Int32 i_pid;

        public SQLDB()
            : base()
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }

        public SQLDB(String server, String dbName, String username, String password)
            : base(server, dbName, username, password)
        {
            i_pid = System.Diagnostics.Process.GetCurrentProcess().Id;
        }


        public DataTable selectVUAndConnections(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select dateg, sum(virtualusers) as virtualusers, sum(connections) as connections from (select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, AVG(virtualusers) AS virtualusers, AVG(connections) AS connections " +
                "from VU " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120), pid) as u  group by u.dateg order by u.dateg";

            return Select(SQL);
        }

        public DataTable selectRequests(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, Count(uri) AS requests, cast((Count(uri)/60) as bigint) AS rps " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";

            return Select(SQL);
        }

        public DataTable selectErrors(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, cast(Count(uri) as bigint) AS errors " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' and statusCode <> 200 " +
                "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";

            return Select(SQL);
        }

        public DataTable selectThroughput(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, sum(bytesReceived) AS bytes, cast(((sum(bytesReceived) * 8)/60) as bigint) mbps " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' " +
                "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";

            return Select(SQL);
        }

        public DataTable selectResponseTime(String testId, DateTime dStart, DateTime dEnd, Uri target)
        {

            String SQL = "select convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) AS dateg, CAST(avg(time) AS BIGINT) AS time " +
                "from WebResult " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "and testID = '" + testId + "' ";
                if (target != null)
                    SQL += "and uri = '" + target.AbsoluteUri + "' ";
                SQL += "group by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120) " +
                "order by convert(datetime,convert(varchar(16), dateg, 120) + ':00',120)";

            return Select(SQL);
        }

        public DataTable selectGeneral(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select  "+
                "VU = (select sum(virtualUsers) from (select MAX(virtualUsers) as virtualUsers, pid from VU where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by pid) as u) " +
                ",connections = (select sum(connections) from (select MAX(connections) as connections, pid from VU where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by pid) as u) " +
                ",throughput = (select cast(((sum(bytesReceived) * 8)/DATEDIFF(SECOND, MIN(date), MAX(date))) as bigint) from WebResult  where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') " +
                ",bytesReceived = (select sum(bytesReceived) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') "+
                ",requests = (select cast(count(*) as bigint) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') "+
                ",rps = (select cast((count(*) / DATEDIFF(SECOND, MIN(date), MAX(date))) as bigint) from WebResult where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "')";

            return Select(SQL);
        }


        public DataTable selectContentTypeDistribution(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select  " +
                "	total = (select cast(COUNT(*) as bigint) from WebResult where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') " +
                "	,qty = cast(COUNT(*) as bigint) " +
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
                "	total = (select cast(sum(time) as bigint) as time from (select avg(time) as time, contentType from WebResult where statusCode = 200  and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' group by contentType) as u) " +
                "	,time = cast(avg(time) as bigint) " +
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
                "	total = (select cast(sum(bytesReceived) as bigint) from WebResult where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "') " +
                "	,bytesReceived = cast(sum(bytesReceived) as bigint) " +
                "	, contentType " +
                "from WebResult " +
                "where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by contentType " +
                "order by contentType";

            return Select(SQL);
        }



        public DataTable selectTopTime(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select top 25 uri, contentType, cast(AVG(time) as bigint) time " +
                "from WebResult " +
                "where statusCode = 200 and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by uri, contentType " +
                "order by AVG(time) desc, uri, contentType";

            return Select(SQL);
        }


        public DataTable selectTopHit(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select top 25  " +
                "	uri " +
                "	,success = (select COUNT(*) from WebResult w1 where w1.statusCode = 200 and w1.uri = wr.uri and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' ) " +
                "	,error = (select COUNT(*) from WebResult w1 where w1.statusCode <> 200 and w1.uri = wr.uri and date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' ) " +
                "from WebResult wr " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by uri " +
                "order by Count(*) desc";

            return Select(SQL);
        }

        public DataTable selectTopBytes(String testId, DateTime dStart, DateTime dEnd)
        {

            String SQL = "select top 25  " +
                "	uri " +
                "	,SUM(bytesReceived) AS bytesReceived " +
                "	,Count(bytesReceived) AS qty " +
                "from WebResult wr " +
                "where date between '" + dStart.ToString("yyyy-MM-dd HH:mm:ss") + "' and '" + dEnd.ToString("yyyy-MM-dd HH:mm:ss") + "' " +
                "group by uri " +
                "order by SUM(bytesReceived) desc";

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



        public void insertMessages(String testId, String title, String text)
        {

            SqlParameterCollection par = GetSqlParameterObject();
            par.Add("@date", System.Data.SqlDbType.DateTime).Value = DateTime.Now;
            par.Add("@pid", System.Data.SqlDbType.BigInt).Value = System.Diagnostics.Process.GetCurrentProcess().Id;
            par.Add("@testId", System.Data.SqlDbType.VarChar, testId.Length).Value = testId;
            par.Add("@title", System.Data.SqlDbType.VarChar, title.Length).Value = title;
            par.Add("@text", System.Data.SqlDbType.VarChar, text.Length).Value = text;

            ExecuteNonQuery("insert into Messages ([date] ,[pid] ,[testId] ,[title] ,[text]) values (@date ,@pid ,@testId ,@title ,@text)", System.Data.CommandType.Text, par, null);

            
        }

        public void insertOptimization(String testId, Uri uri, Int64 originalLength, Int64 optimizedLength)
        {

            SqlParameterCollection par = GetSqlParameterObject();
            par.Add("@date", System.Data.SqlDbType.DateTime).Value = DateTime.Now;
            par.Add("@pid", System.Data.SqlDbType.BigInt).Value = System.Diagnostics.Process.GetCurrentProcess().Id;
            par.Add("@testId", System.Data.SqlDbType.VarChar, testId.Length).Value = testId;
            par.Add("@uri", System.Data.SqlDbType.VarChar, uri.AbsoluteUri.Length).Value = uri.AbsoluteUri;
            par.Add("@originalLength", System.Data.SqlDbType.BigInt).Value = originalLength;
            par.Add("@optimizedLength", System.Data.SqlDbType.BigInt).Value = optimizedLength;

            ExecuteNonQuery("insert into Optimization ([date] ,[pid] ,[testId] ,[uri] ,[originalLength] ,[optimizedLength]) values (@date ,@pid ,@testId ,@uri ,@originalLength ,@optimizedLength)", System.Data.CommandType.Text, par, null);


        }


    }
}
*/