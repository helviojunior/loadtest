using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.ComponentModel;
using SafeTrend.Data;

namespace SafeTrend.Data.SqlClient
{
    [Serializable()]
    public class SqlBase : DbBase
    {
        [NonSerialized()]
        private SqlConnection connection;
        
        [NonSerialized()]
        private Int32 pid;
        
        [NonSerialized()]
        private Int32 timeout;
        
        [NonSerialized()]
        private String connectionString;
        
        [NonSerialized()]
        private Boolean externalConnection;
        
        [NonSerialized()]
        private SqlTransaction transaction;

        public Int32 Timeout { get { return timeout; } set { timeout = value; } }
        public SqlConnection Connection { get { return connection; } set { connection = value; this.externalConnection = true; } }
        public String ConnectionString { get { return connectionString; } }

        public SqlBase()
        {
            this.pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            this.timeout = 300;
            this.externalConnection = false;
        }

        public SqlBase(SqlConnection connection)
            :this()
        {
            this.connection = connection;
            if (this.connection.State != ConnectionState.Open)
                this.connection.Open();

            this.externalConnection = true;
        }

        public SqlBase(String server, String dbName) :
            this(server, dbName, false) { }

        public SqlBase(String server, String dbName, Boolean useSharedMemory)
            : this()
        {
            if (useSharedMemory)
                connectionString = string.Format("Data Source={0};Initial Catalog={1};Integrated Security=True;Network Library=dbmslpcn;", server, dbName);
            else
                connectionString = string.Format("Data Source={0};Initial Catalog={1};Trusted_Connection=True;", server, dbName);
        }

        public SqlBase(String server, String dbName, String username, String password)
            : this()
        {
            connectionString = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3};", server, dbName, username, password);
            this.openDB();
        }

        public SqlBase(String connectionString)
            : this()
        {
            this.connectionString = connectionString;
            this.openDB();
        }
        
        public SqlConnection openDB()
        {
            if ((this.connection != null) && (this.connection.State == ConnectionState.Open))
                return this.connection;

            this.connection = new SqlConnection(this.connectionString);
            if (this.connection.State == ConnectionState.Closed)
            {
                this.connection.Open();
            }

            return this.connection;
        }

        public override void Dispose()
        {
            if (this.externalConnection)
                return;
            
            closeDB();
            this.connection = null;
        }

        public void closeDB()
        {
            if (this.externalConnection)
                return;

            try
            {
                this.connection.Close();
                this.connection = null;
            }
            catch { }
        }

        public override Object BeginTransaction()
        {
            if (this.transaction == null)
                this.transaction = this.connection.BeginTransaction();

            return this.transaction;
        }

        public override void Commit()
        {
            transaction.Commit();
            this.transaction = null;
        }

        public override void Rollback()
        {
            transaction.Rollback();
            this.transaction = null;
        }

        //Métodos nativos da classe
        public override DataTable GetSchema(String tableName)
        {
            if ((connection == null) || (connection.State == ConnectionState.Closed))
            {
                i_lastError = "Connection is null";
                return null;
            }

            DataTable tst = new DataTable();
            SqlDataAdapter adp = new SqlDataAdapter("SELECT top 0 * FROM " + tableName, connection);
            adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            tst = adp.FillSchema(tst, SchemaType.Source);

            return tst;
        }

        public override void BulkCopy(DataTable source, String table, Object transaction)
        {
            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            if ((transaction != null) && (!(transaction is SqlTransaction)))
                throw new Exception("Transaction is not a SqlTransaction");

            if (transaction == null)
            {
                using (SqlBulkCopy bulk = new SqlBulkCopy(connection))
                {
                    bulk.DestinationTableName = table;
                    bulk.WriteToServer(source);
                }
            }
            else
            {
                using (SqlBulkCopy bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, (SqlTransaction)transaction))
                {
                    bulk.DestinationTableName = table;
                    bulk.WriteToServer(source);
                }
            }
        }


        public override T ExecuteScalar<T>(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {
            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            if ((transaction != null) && (!(transaction is SqlTransaction)))
                throw new Exception("Transaction is not a SqlTransaction");

            SqlCommand cmd = new SqlCommand(TrataMacros(command), connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = timeout;

            try
            {

                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (DbParameter par in parameters)
                        cmd.Parameters.Add(par.ParameterName, GetDBType(par.Type), par.Size).Value = par.Value;
                }

                if (transaction != null)
                    cmd.Transaction = (SqlTransaction)transaction;

                DebugQuery("BaseDB.ExecuteScalar", cmd.CommandText, parameters, transaction);
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(cmd.ExecuteScalar().ToString());
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery("BaseDB.ExecuteScalar", cmd.CommandText, parameters, transaction);
                throw ex;
            }
            finally
            {
                //if (Parameters != null) Parameters.Clear();
                //Parameters = null;

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }

        public override DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {

            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            if ((transaction != null) && (!(transaction is SqlTransaction)))
                throw new Exception("Transaction is not a SqlTransaction");

            SqlCommand cmd = new SqlCommand(TrataMacros(command), connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = timeout;

            SqlDataAdapter da = null;
            DataSet ds = null;
            try
            {

                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (DbParameter par in parameters)
                        cmd.Parameters.Add(par.ParameterName, GetDBType(par.Type), par.Size).Value = par.Value;
                }

                if (transaction != null)
                    cmd.Transaction = (SqlTransaction)transaction;

                DebugQuery("BaseDB.ExecuteDataTable", cmd.CommandText, parameters, transaction);

                da = new SqlDataAdapter(cmd);
                ds = new DataSet();
                da.Fill(ds, "data");
                da.Dispose();

                DataTable tmp = ds.Tables["data"];

                return tmp;

            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery("BaseDB.ExecuteDataTable", cmd.CommandText, parameters, transaction);
                throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }

        public override void ExecuteNonQuery(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {

            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            if ((transaction != null) && (!(transaction is SqlTransaction)))
                throw new Exception("Transaction is not a SqlTransaction");

            SqlCommand cmd = new SqlCommand(TrataMacros(command), connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = timeout;

            try
            {

                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (DbParameter par in parameters)
                        cmd.Parameters.Add(par.ParameterName, GetDBType(par.Type), par.Size).Value = par.Value;
                }

                if (transaction != null)
                    cmd.Transaction = (SqlTransaction)transaction;

                DebugQuery("BaseDB.ExecuteNonQuery", cmd.CommandText, parameters, transaction);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                i_lastError = ex.Message + DebugQuery("BaseDB.ExecuteNonQuery", cmd.CommandText, parameters, transaction);
                throw ex;
            }
            finally
            {
                //if (Parameters != null) Parameters.Clear();
                //Parameters = null;

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }

        public override void CreateDatabase(String dbName) {

            ExecuteNonQuery("use [master]");
            ExecuteNonQuery("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'" + dbName + "') create database [" + dbName + "]");
            ExecuteNonQuery("use [" + dbName + "]");
        }

        public override void DropDatabase(String dbName) {

            ExecuteNonQuery("use [master]");

            //Finaliza as conexões com o DB que será excluido
            DataTable process = ExecuteDataTable("SELECT CONVERT (VARCHAR(25), spid) AS spid FROM master..sysprocesses pr INNER JOIN master..sysdatabases db ON pr.dbid = db.dbid WHERE db.name = '" + dbName + "'");
            foreach (DataRow dr in process.Rows)
            {
                try
                {
                    ExecuteNonQuery("kill " + dr["spid"].ToString());
                }
                catch { }
            }

            //Exclui o DB
            ExecuteNonQuery("IF EXISTS (SELECT name FROM sys.databases WHERE name = N'" + dbName + "') drop database  [" + dbName + "]");

        }


        private SqlDbType GetDBType(System.Type theType)
        {
            SqlParameter param;
            System.ComponentModel.TypeConverter tc;
            param = new SqlParameter();
            tc = System.ComponentModel.TypeDescriptor.GetConverter(param.DbType);
            if (tc.CanConvertFrom(theType))
            {
                param.DbType = (DbType)tc.ConvertFrom(theType.Name);
            }
            else
            {
                // try to forcefully convert
                try
                {
                    param.DbType = (DbType)tc.ConvertFrom(theType.Name);
                }
                catch (Exception e)
                {
                    // ignore the exception
                }
            }
            return param.SqlDbType;
        }

        private String TrataMacros(String command)
        {
            String cmd = command;
            cmd = cmd.Replace("|nolock|", " with(nolock) ");

            return cmd;
        }



        //Métodos passando connection
        public DataTable GetSchema(SqlConnection conn, String tableName) { this.connection = conn; return GetSchema(tableName); }

        public void BulkCopy(SqlConnection conn, DataTable source, String table) { BulkCopy(conn, source, table, null); }
        public void BulkCopy(SqlConnection conn, DataTable source, String table, Object transaction) { this.connection = conn; BulkCopy(source, table, transaction); }

        public T ExecuteScalar<T>(SqlConnection conn, String command) { this.connection = conn; return ExecuteScalar<T>(command, CommandType.Text, null, null); }
        public T ExecuteScalar<T>(SqlConnection conn, String command, DbParameterCollection parameters) { this.connection = conn; return ExecuteScalar<T>(command, CommandType.Text, parameters, null); }
        public T ExecuteScalar<T>(SqlConnection conn, String command, Object transaction) { this.connection = conn; return ExecuteScalar<T>(command, CommandType.Text, null, transaction); }
        public T ExecuteScalar<T>(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters) { this.connection = conn; return ExecuteScalar<T>(command, commandType, parameters, null); }
        public T ExecuteScalar<T>(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters, Object transaction) { this.connection = conn; return ExecuteScalar<T>(command, commandType, parameters, transaction); }

        public DataTable ExecuteDataTable(SqlConnection conn, String command) { this.connection = conn; return ExecuteDataTable(command, CommandType.Text, null, null); }
        public DataTable ExecuteDataTable(SqlConnection conn, String command, DbParameterCollection parameters) { this.connection = conn; return ExecuteDataTable(command, CommandType.Text, parameters, null); }
        public DataTable ExecuteDataTable(SqlConnection conn, String command, Object transaction) { this.connection = conn; return ExecuteDataTable(command, CommandType.Text, null, transaction); }
        public DataTable ExecuteDataTable(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters) { this.connection = conn; return ExecuteDataTable(command, commandType, parameters, null); }
        public DataTable ExecuteDataTable(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters, Object transaction) { this.connection = conn; return ExecuteDataTable(command, commandType, parameters, transaction); }

        //Mesmos métodos do ExecuteDataTable
        public DataTable Select(SqlConnection conn, String command) { this.connection = conn; return ExecuteDataTable(command, CommandType.Text, null, null); }
        public DataTable Select(SqlConnection conn, String command, DbParameterCollection parameters) { this.connection = conn; return ExecuteDataTable(command, CommandType.Text, parameters, null); }
        public DataTable Select(SqlConnection conn, String command, Object transaction) { this.connection = conn; return ExecuteDataTable(command, CommandType.Text, null, transaction); }
        public DataTable Select(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters) { this.connection = conn; return ExecuteDataTable(command, commandType, parameters, null); }
        public DataTable Select(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters, Object transaction) { this.connection = conn; return ExecuteDataTable(command, commandType, parameters, transaction); }

        public void ExecuteNonQuery(SqlConnection conn, String command) { this.connection = conn; ExecuteNonQuery(command, CommandType.Text, null, null); }
        public void ExecuteNonQuery(SqlConnection conn, String command, DbParameterCollection parameters) { this.connection = conn; ExecuteNonQuery(command, CommandType.Text, parameters, null); }
        public void ExecuteNonQuery(SqlConnection conn, String command, Object transaction) { this.connection = conn; ExecuteNonQuery(command, CommandType.Text, null, transaction); }
        public void ExecuteNonQuery(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters) { this.connection = conn; ExecuteNonQuery(command, commandType, parameters, null); }
        public void ExecuteNonQuery(SqlConnection conn, String command, CommandType commandType, DbParameterCollection parameters, Object transaction) { this.connection = conn; ExecuteNonQuery(command, commandType, parameters, transaction); }

    }
}
