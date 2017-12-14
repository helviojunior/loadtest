using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Configuration;
using SafeTrend.Data.SQLite;
using SafeTrend.Data.SqlClient;

namespace SafeTrend.Data
{
    [Serializable()]
    public abstract class DbBase : IDisposable
    {
        [NonSerialized()]
        protected Boolean i_debug;
        
        [NonSerialized()]
        protected String i_lastError;

        public String LastDBError { get { return i_lastError; } }
        public Boolean Debug { get { return i_debug; } set { i_debug = value; } }

        public static DbParameterCollection GetParameterObject()
        {
            return new DbParameterCollection();
        }

        public abstract void Dispose();

        internal String DebugQuery(String source, String command, DbParameterCollection parameters)
        {
            return DebugQuery(source, command, parameters, null, false);
        }

        internal String DebugQuery(String source, String command, DbParameterCollection parameters, Object transaction)
        {
            return DebugQuery(source, command, parameters, transaction, false);
        }

        internal String DebugQuery(String source, String command, DbParameterCollection parameters, Object transaction, Boolean force)
        {

            if (!i_debug && !force)
                return "";

            String debug = "/****** Source:  " + source + "    Script Date: " + DateTime.Now.ToString("yyyy-mm-dd HH:mm:ss.ffffff") + " ******/" + Environment.NewLine;

            if (parameters != null)
            {
                foreach (DbParameter par in parameters)
                {

                    debug += "DECLARE " + par.ParameterName + " " + par.Type.ToString();
                    if (par.Size > 0)
                        debug += "(" + par.Size + ")";
                    debug += Environment.NewLine;
                    switch (par.Type.ToString().ToLower())
                    {
                        case "int64":
                        case "long":
                        case "int32":
                        case "int":
                        case "int16":
                            debug += "SET " + par.ParameterName + " = " + par.Value + Environment.NewLine;
                            break;

                        case "boolean":
                        case "bool":
                            debug += "SET " + par.ParameterName + " = " + ((Boolean)par.Value ? "1" : "0") + Environment.NewLine;
                            break;

                        default:
                            debug += "SET " + par.ParameterName + " = '" + par.Value + "'" + Environment.NewLine;
                            break;
                    }

                }
                debug += Environment.NewLine;
            }


            debug += command + Environment.NewLine;

            debug += "GO" + Environment.NewLine + Environment.NewLine;

            if (!i_debug)
                return debug;

            DbParameterCollection col = new DbParameterCollection();
            col.Add("@text", typeof(String)).Value = debug;

            try
            {
                ExecuteNonQuery("insert into debug (text) values (@text)", CommandType.Text, col, transaction);
            }
            catch { }

            return debug;
        }

        public abstract DataTable GetSchema(String tableName);

        public void BulkCopy(DataTable source, String table) { BulkCopy(source, table, null); }
        public abstract void BulkCopy(DataTable source, String table, Object transaction);

        public T ExecuteScalar<T>(String command) { return ExecuteScalar<T>(command, CommandType.Text, null, null); }
        public T ExecuteScalar<T>(String command, DbParameterCollection parameters) { return ExecuteScalar<T>(command, CommandType.Text, parameters, null); }
        public T ExecuteScalar<T>(String command, Object transaction) { return ExecuteScalar<T>(command, CommandType.Text, null, transaction); }
        public T ExecuteScalar<T>(String command, CommandType commandType, DbParameterCollection parameters) { return ExecuteScalar<T>(command, commandType, parameters, null); }
        public abstract T ExecuteScalar<T>(String command, CommandType commandType, DbParameterCollection parameters, Object transaction);

        public DataTable ExecuteDataTable(String command) { return ExecuteDataTable(command, CommandType.Text, null, null); }
        public DataTable ExecuteDataTable(String command, DbParameterCollection parameters) { return ExecuteDataTable(command, CommandType.Text, parameters, null); }
        public DataTable ExecuteDataTable(String command, Object transaction) { return ExecuteDataTable(command, CommandType.Text, null, transaction); }
        public DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection parameters) { return ExecuteDataTable(command, commandType, parameters, null); }
        public abstract DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection parameters, Object transaction);

        //Mesmos métodos do ExecuteDataTable
        public DataTable Select(String command) { return ExecuteDataTable(command, CommandType.Text, null, null); }
        public DataTable Select(String command, DbParameterCollection parameters) { return ExecuteDataTable(command, CommandType.Text, parameters, null); }
        public DataTable Select(String command, Object transaction) { return ExecuteDataTable(command, CommandType.Text, null, transaction); }
        public DataTable Select(String command, CommandType commandType, DbParameterCollection parameters) { return ExecuteDataTable(command, commandType, parameters, null); }
        public DataTable Select(String command, CommandType commandType, DbParameterCollection parameters, Object transaction) { return ExecuteDataTable(command, commandType, parameters, transaction); }

        public void ExecuteNonQuery(String command) { ExecuteNonQuery(command, CommandType.Text, null, null); }
        public void ExecuteNonQuery(String command, DbParameterCollection parameters) { ExecuteNonQuery(command, CommandType.Text, parameters, null); }
        public void ExecuteNonQuery(String command, Object transaction) { ExecuteNonQuery(command, CommandType.Text, null, transaction); }
        public void ExecuteNonQuery(String command, CommandType commandType, DbParameterCollection parameters) { ExecuteNonQuery(command, commandType, parameters, null); }
        public abstract void ExecuteNonQuery(String command, CommandType commandType, DbParameterCollection parameters, Object transaction);

        public abstract void CreateDatabase(String dbName);
        public abstract void DropDatabase(String dbName);

        public abstract Object BeginTransaction();
        public abstract void Commit();
        public abstract void Rollback();

        public static DbBase InstanceFromConfig(ConnectionStringSettings connectionString)
        {
            return InstanceFromConfig(new DbConnectionString(connectionString));
        }

        public static DbBase InstanceFromConfig(DbConnectionString connectionString)
        {
            switch (connectionString.ProviderName.ToLower())
            {
                case "sqlite":
                case "system.data.sqlite":
                    return new SqliteBase(connectionString.ConnectionString);

                case "sqlclient":
                case "system.data.sqlclient":
                    return new SqlBase(connectionString.ConnectionString);

                default:
                    throw new NotImplementedException(string.Format("The provider '{0}' is not supported yet", connectionString.ProviderName));
            }
        }
    }
}
