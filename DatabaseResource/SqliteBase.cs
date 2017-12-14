using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.IO;
using System.ComponentModel;
using SafeTrend.Data;

namespace SafeTrend.Data.SQLite
{
    public class SqliteBase : DbBase
    {
        private SQLiteConnection connection;
        private Int32 pid;
        private Int32 timeout;
        private String connectionString;

        public SqliteBase()
        {
            this.pid = System.Diagnostics.Process.GetCurrentProcess().Id;
            this.timeout = 300;
        }

        public SqliteBase(SQLiteConnection connection)
            :this()
        {
            this.connection = connection;
            if (this.connection.State != ConnectionState.Open)
                this.connection.Open();
        }

        public SqliteBase(FileInfo fileName)
            : this()
        {
            this.connectionString = string.Format("Data Source={0};Version=3;", fileName.FullName);
            this.openDB();
        }

        public SqliteBase(String connectionString)
            : this()
        {
            this.connectionString = connectionString;
            this.openDB();
        }
        
        public SQLiteConnection openDB()
        {
            if ((this.connection != null) && (this.connection.State == ConnectionState.Open))
                return this.connection;

            this.connection = new SQLiteConnection(this.connectionString);
            if (this.connection.State == ConnectionState.Closed)
            {
                this.connection.Open();
            }

            return this.connection;
        }

        public override void Dispose()
        {
            this.connection = null;
            closeDB();
        }

        public void closeDB()
        {
            try
            {
                this.connection.Close();
                this.connection = null;
            }
            catch { }
        }

        public override DataTable GetSchema(String tableName)
        {
            if ((connection == null) || (connection.State == ConnectionState.Closed))
            {
                i_lastError = "Connection is null";
                return null;
            }

            DataTable tst = new DataTable();
            SQLiteDataAdapter adp = new SQLiteDataAdapter("SELECT top 0 * FROM " + tableName, connection);
            adp.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            tst = adp.FillSchema(tst, SchemaType.Source);

            return tst;
        }

        public override void BulkCopy(DataTable source, String table, Object transaction)
        {
            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            List<String> columns = new List<string>();
            List<String> values = new List<string>();

            foreach(DataColumn dc in source.Columns){
                columns.Add("[" + dc.ColumnName + "]");
                values.Add("@" + dc.ColumnName);
            }

            String insertCmd = "INSERT INTO [" + table + "] (" + String.Join(",",columns) + ") VALUES (" + String.Join(",",values) + ")";
            columns.Clear();
            values.Clear();


            foreach (DataRow dr in source.Rows)
            {
                using (DbParameterCollection par = new DbParameterCollection())
                {

                    foreach (DataColumn dc in source.Columns)
                        par.Add(dc.ColumnName, dc.DataType).Value = dr[dc.ColumnName];

                    ExecuteNonQuery(insertCmd, par);
                }
            }

        }


        public override T ExecuteScalar<T>(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {
            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            SQLiteCommand cmd = new SQLiteCommand(TrataMacros(command), connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = timeout;

            try
            {

                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (DbParameter par in parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, GetDBType(par.Type), par.Size).Value = par.Value;
                        //cmd.CommandText = cmd.CommandText.Replace(par.ParameterName, "?");
                    }
                }

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
                try
                {
                    if (cmd != null) cmd.Dispose();
                    cmd = null;
                }
                catch { }
            }
        }

        public override DataTable ExecuteDataTable(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {

            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            SQLiteCommand cmd = new SQLiteCommand(TrataMacros(command), connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = timeout;

            SQLiteDataAdapter da = null;
            DataSet ds = null;
            try
            {

                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (DbParameter par in parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, GetDBType(par.Type), par.Size).Value = par.Value;
                        //cmd.CommandText = cmd.CommandText.Replace(par.ParameterName, "?");
                    }
                }

                DebugQuery("BaseDB.ExecuteDataTable", cmd.CommandText, parameters, transaction);

                da = new SQLiteDataAdapter(cmd);
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
                try
                {
                    if (cmd != null) cmd.Dispose();
                    cmd = null;
                }
                catch { }
            }
        }

        public override void ExecuteNonQuery(String command, CommandType commandType, DbParameterCollection parameters, Object transaction)
        {

            if ((connection == null) || (connection.State == ConnectionState.Closed))
                throw new Exception("Connection is null");

            SQLiteCommand cmd = new SQLiteCommand(TrataMacros(command), connection);
            cmd.CommandType = commandType;
            cmd.CommandTimeout = timeout;

            try
            {

                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (DbParameter par in parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, GetDBType(par.Type), par.Size).Value = par.Value;
                        //cmd.CommandText = cmd.CommandText.Replace(par.ParameterName, "?");
                    }
                }

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

                try
                {
                    if (cmd != null) cmd.Dispose();
                    cmd = null;
                }
                catch { }
            }
        }

        public override void CreateDatabase(String dbName) { }
        public override void DropDatabase(String dbName) {
            String dbfName = connection.Database;
            closeDB();
            File.Delete(dbfName);
        }

        public override Object BeginTransaction() { return null; }
        public override void Commit() { }
        public override void Rollback() { }

        private String TrataMacros(String command)
        {
            String cmd = command;
            cmd = cmd.Replace("|nolock|", "");

            return cmd;
        }


        private DbType GetDBType(System.Type theType)
        {
            SQLiteParameter param;
            System.ComponentModel.TypeConverter tc;
            param = new SQLiteParameter();
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
            return param.DbType;
        }
    }
}
