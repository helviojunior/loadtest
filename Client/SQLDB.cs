using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DBManager;
using System.Data;
using System.Data.SqlClient;

namespace Client
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

        public DataTable selectConnectionsError(String testId)
        {
            String SQL = "SELECT * " +
                    "FROM ConnectionsError " +
                    "WHERE test_id = '" + testId + "' " +
                    "ORDER BY [date];";

            return Select(SQL);
        }

        public DataTable selectInterfaceMonitorHosts(String testId)
        {
            String SQL = "SELECT host " +
                    "FROM interfacemonitor " +
                //"WHERE test_id = '" + testId + "' " +
                    "GROUP BY host " +
                    "ORDER BY host;";

            return Select(SQL);
        }

        public DataTable selectInterfaceMonitorInterfaces(String host, String testId)
        {
            String SQL = "SELECT interface " +
                    "FROM interfacemonitor " +
                    "WHERE host = '" + host + "' and interface <> 'Error'" +
                    "GROUP BY interface " +
                    "ORDER BY interface;";

            return Select(SQL);
        }

        public DataTable selectInterfaceMonitorEvents(String host, String ifName, String testId)
        {

            String SQL = "SELECT * " +
                    "FROM interfacemonitor " +
                    "WHERE host = '" + host + "' and (interface = '" + ifName + "' or interface = 'Error')" +
                    "ORDER BY [date];";

            return Select(SQL);
        }

        public DataTable selectCPUMemoryHosts(String testId)
        {
            String SQL = "SELECT host " +
                    "FROM cpumemory " +
                    "GROUP BY host " +
                    "ORDER BY host;";

            return Select(SQL);
        }

        public DataTable selectCPUMemoryEvents(String host, String testId)
        {

            String SQL = "SELECT * " +
                    "FROM cpumemory " +
                    "WHERE host = '" + host + "'" +
                    "ORDER BY [date];";

            return Select(SQL);
        }


        public DataTable selectStatic(Int32 type, String testId)
        {

            String SQL = "";

            if (type == 1)
            {
                //Busca agrupado por data
                SQL = "SELECT dateg, Sum(bytesSent) AS bytesSent, Sum(bytesReceived) AS bytesReceived, Sum(request) AS request, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                         "FROM StaticStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY dateg " +
                         "ORDER BY dateg;";
            }
            else if (type == 2)
            {
                SQL = "SELECT date, date as dateg, connections AS connections, bytesSent AS bytesSent, bytesReceived AS bytesReceived, request AS request, ConnectionsErrors AS ConnectionsErrors, MD5errors AS MD5errors " +
                            "FROM StaticStatistics " +
                            "WHERE testID = '" + testId + "' " +
                            "ORDER BY date;";

            }
            else if (type == 3)
            {
                //Busca somente a quantidade de conexões
                SQL = "SELECT dateg , SUM(connections) AS connections, SUM(ConnectionsErrors) AS ConnectionsErrors, SUM(MD5errors) AS MD5errors " +
                        "FROM (SELECT dateg, rangeName, Max(connections) AS connections, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                         "FROM StaticStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY dateg, pid, rangeName, testId) as t " +
                         "GROUP BY dateg " +
                         "ORDER BY dateg";

            }

            else if (type == 4)
            {
                //Busca somente os nomes dos ranges
                SQL = "SELECT rangeName " +
                         "FROM StaticStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY rangeName";
            }

            return Select(SQL);
        }

        public DataTable selectStaticByName(String rangeName, String testId)
        {

            String SQL = "";

            //Busca somente a quantidade de conexões pelo range de IP
            SQL = "SELECT dateg, rangeName, SUM(connections) AS connections, SUM(ConnectionsErrors) AS ConnectionsErrors, SUM(MD5errors) AS MD5errors " +
                    "FROM (SELECT dateg, rangeName, Max(connections) AS connections, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                     "FROM StaticStatistics " +
                     "WHERE rangeName = '" + rangeName + "' " +
                     "AND testID = '" + testId + "' " +
                     "GROUP BY dateg, pid, rangeName) as t " +
                     "GROUP BY dateg, rangeName " +
                     "ORDER BY dateg";

            return Select(SQL);
        }

        public DataTable selectDynamic(Int32 type, String testId)
        {

            String SQL = "";

            if (type == 1)
            {
                //Busca agrupado por data
                SQL = "SELECT dateg, Sum(bytesSent) AS bytesSent, Sum(bytesReceived) AS bytesReceived, Sum(request) AS request, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                         "FROM DynamicStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY dateg " +
                         "ORDER BY dateg;";
            }
            else if (type == 2)
            {
                SQL = "SELECT date, date as dateg, connections AS connections, bytesSent AS bytesSent, bytesReceived AS bytesReceived, request AS request, ConnectionsErrors AS ConnectionsErrors, MD5errors AS MD5errors " +
                            "FROM DynamicStatistics " +
                            "WHERE testID = '" + testId + "' " +
                            "ORDER BY date;";

            }
            else if (type == 3)
            {
                //Busca somente a quantidade de conexões
                SQL = "SELECT dateg , SUM(connections) AS connections, SUM(ConnectionsErrors) AS ConnectionsErrors, SUM(MD5errors) AS MD5errors " +
                        "FROM (SELECT dateg, rangeName, Max(connections) AS connections, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                         "FROM DynamicStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY dateg, pid, rangeName, testId) as t " +
                         "GROUP BY dateg " +
                         "ORDER BY dateg";

            }
            else if (type == 4)
            {
                //Busca somente os nomes dos ranges
                SQL = "SELECT rangeName " +
                         "FROM DynamicStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY rangeName";
            }

            return Select(SQL);
        }


        public DataTable selectDynamicByName(String rangeName, String testId)
        {

            String SQL = "";

            //Busca somente a quantidade de conexões pelo range de IP
            SQL = "SELECT dateg, rangeName, SUM(connections) AS connections, SUM(ConnectionsErrors) AS ConnectionsErrors, SUM(MD5errors) AS MD5errors " +
                    "FROM (SELECT dateg, rangeName, Max(connections) AS connections, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                     "FROM DynamicStatistics " +
                     "WHERE rangeName = '" + rangeName + "' " +
                     "AND testID = '" + testId + "' " +
                     "GROUP BY dateg, pid, rangeName) as t " +
                     "GROUP BY dateg, rangeName " +
                     "ORDER BY dateg";

            return Select(SQL);
        }

        public DataTable selectIdle(Int32 type, String testId)
        {

            String SQL = "";

            if (type == 1)
            {
                //Busca agrupado por data
                SQL = "SELECT dateg, Sum(bytesSent) AS bytesSent, Sum(bytesReceived) AS bytesReceived, Sum(request) AS request, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                         "FROM IdleStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY dateg " +
                         "ORDER BY dateg;";
            }
            else if (type == 2)
            {
                SQL = "SELECT date, date as dateg, connections AS connections, bytesSent AS bytesSent, bytesReceived AS bytesReceived, request AS request, ConnectionsErrors AS ConnectionsErrors, MD5errors AS MD5errors " +
                            "FROM IdleStatistics " +
                            "WHERE testID = '" + testId + "' " +
                            "ORDER BY date;";

            }
            else if (type == 3)
            {
                //Busca somente a quantidade de conexões
                SQL = "SELECT dateg , SUM(connections) AS connections, SUM(ConnectionsErrors) AS ConnectionsErrors, SUM(MD5errors) AS MD5errors " +
                        "FROM (SELECT dateg, rangeName, Max(connections) AS connections, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                         "FROM IdleStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY dateg, pid, rangeName, testId) as t " +
                         "GROUP BY dateg " +
                         "ORDER BY dateg";

            }
            else if (type == 4)
            {
                //Busca somente os nomes dos ranges
                SQL = "SELECT rangeName " +
                         "FROM IdleStatistics " +
                         "WHERE testID = '" + testId + "' " +
                         "GROUP BY rangeName";
            }

            return Select(SQL);
        }


        public DataTable selectIdleByName(String rangeName, String testId)
        {

            String SQL = "";

            //Busca somente a quantidade de conexões pelo range de IP
            SQL = "SELECT dateg, rangeName, SUM(connections) AS connections, SUM(ConnectionsErrors) AS ConnectionsErrors, SUM(MD5errors) AS MD5errors " +
                    "FROM (SELECT dateg, rangeName, Max(connections) AS connections, Sum(ConnectionsErrors) AS ConnectionsErrors, Sum(MD5errors) AS MD5errors " +
                     "FROM IdleStatistics " +
                     "WHERE rangeName = '" + rangeName + "' " +
                     "AND testID = '" + testId + "' " +
                     "GROUP BY dateg, pid, rangeName, testId) as t " +
                     "GROUP BY dateg, rangeName " +
                     "ORDER BY dateg";

            return Select(SQL);
        }

        //

        public DataTable selectICMP(String testId)
        {

            String SQL = "SELECT * FROM icmp ORDER BY [date]";

            return Select(SQL);
        }


        public DataTable selectICMPHB(String testId)
        {

            String SQL = "SELECT * FROM icmphb ORDER BY [date]";

            return Select(SQL);
        }

        public DataTable selectEvents(String testId)
        {

            String SQL = "SELECT * FROM [events] WHERE test_ID = '" + testId + "' ORDER BY [id]";

            return Select(SQL);
        }

        public DataTable selectIcmpTimeoff(String testId)
        {

            String SQL = "SELECT * FROM [icmptimeoff] ORDER BY [id]";

            return Select(SQL);
        }

        public DataTable selectMarks(String testId)
        {

            String SQL = "SELECT * FROM [events] WHERE test_ID = '" + testId + "' AND event_text like 'Efetuando restart%' or event_text like 'Desligando %' or event_text like 'Ligando %' ORDER BY [date]";

            return Select(SQL);
        }

        public DataTable selectDateRange(String testId)
        {

            String SQL = "SELECT max([date]) as final, min([date]) as inicial FROM [events] WHERE test_ID = '" + testId + "' ";

            return Select(SQL);
        }

    }
}
