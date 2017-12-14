using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Diagnostics;

namespace DBManager
{
    public abstract class BaseDB : IDisposable
    {
        private String i_server;
        private String i_username;
        private String i_password;
        private String i_dbname;
        private SqlConnection i_cn;
        private Boolean i_Opened;
        private String i_ConnectionString;

        public SqlConnection conn { get { return i_cn; } }
        //public Boolean isOpenned { get { return i_Opened; } }

        public BaseDB()
        {
            i_Opened = false;
        }

        public BaseDB(String server, String dbName, String username, String password)
        {
            i_Opened = false;
            i_server = server;
            i_username = username;
            i_password = password;
            i_dbname = dbName;
            i_ConnectionString = string.Format("Data Source={0};Initial Catalog={1};User Id={2};Password={3};", i_server, i_dbname, i_username, i_password);
        }


        public SqlConnection openDB()
        {
            i_cn = new SqlConnection(i_ConnectionString);
            if (i_cn.State == ConnectionState.Closed)
            {
                i_cn.Open();
            }

            i_Opened = true;
            return i_cn;
        }

        public void Dispose()
        {
            i_ConnectionString = null;

            closeDB();
        }

        public void closeDB()
        {
            i_Opened = false;
            try
            {
                i_cn.Close();
                i_cn = null;
            }
            catch { }
        }

        public DataTable Select(String SQL)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            return Select(SQL, i_cn);
        }

        public DataTable Select(String SQL, SqlConnection conn)
        {

            if ((conn == null) || (conn.State == ConnectionState.Closed))
            {
                return null;
            }

            SqlCommand select = null;
            SqlDataAdapter da = null;
            DataSet ds = null;
            try
            {
                select = new SqlCommand(SQL, conn);

                select.CommandType = CommandType.Text;

                da = new SqlDataAdapter(select);
                ds = new DataSet();
                da.Fill(ds, "data");
                da.Dispose();

                DataTable tmp = ds.Tables["data"];

                return tmp;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("AkClient", "Falha no select (" + SQL + ") " + ex.Message + ex.StackTrace, EventLogEntryType.Error);

                if (select != null) select.Dispose();
                select = null;

                if (da != null) da.Dispose();
                da = null;

                if (ds != null) ds.Dispose();
                ds = null;

                return null;
            }
        }


        public void BulkCopy(DataTable source, String table)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            BulkCopy(source, table, i_cn);

        }

        public void BulkCopy(DataTable source, String table, SqlConnection conn)
        {
            using (SqlBulkCopy bulk = new SqlBulkCopy(conn))
            {
                bulk.BulkCopyTimeout = 90;
                bulk.DestinationTableName = table;
                bulk.WriteToServer(source);
            }
        }

        public DataTable selectAllFrom(String tableName, String filter, SqlConnection conn)
        {
            String SQL = "SELECT * " +
                         "FROM [" + tableName + "]";

            if ((filter != null) && (filter != ""))
                SQL += " WHERE " + filter;

            return Select(SQL, conn);
        }

        public SqlParameterCollection GetSqlParameterObject()
        {
            return (SqlParameterCollection)typeof(SqlParameterCollection).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke(null);
        }

        public void Insert(String insertSQL, SqlParameterCollection Parameters)
        {
            SqlConnection conn = new SqlConnection(i_ConnectionString);
            conn.Open();

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = null;
            SqlDataReader dr = null;
            try
            {
                cmd = new SqlCommand(insertSQL, conn);
                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();
                if (conn != null) conn.Close();
                if (conn != null) conn.Dispose();

                cmd = null;
            }
        }

        public void Insert2(String insertSQL, SqlParameterCollection Parameters)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = new SqlCommand(insertSQL, i_cn);
            SqlDataReader dr = null;
            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();
                
                cmd = null;
            }
        }


        public Object ExecuteSQLScalar(String sql, SqlParameterCollection parameters, CommandType commandType)
        {
            Object ret = null;

            SqlCommand cmd = null;
            SqlDataReader dr = null;
            try
            {

                cmd = new SqlCommand(sql, conn);
                cmd.CommandType = commandType;
                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                ret = cmd.ExecuteScalar();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();

                cmd = null;
            }

            return ret;
        }

        public void ExecuteSQL(String sql, SqlParameterCollection parameters, CommandType commandType)
        {

            SqlCommand cmd = null;
            SqlDataReader dr = null;
            try
            {
                cmd = new SqlCommand(sql, conn);
                cmd.CommandType = commandType;
                if (parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in parameters)
                    {
                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                if (dr != null) dr.Close();

                cmd = null;
            }
        }


        public void ExecuteNonQuery(String command, CommandType commandType, SqlParameterCollection Parameters)
        {
            ExecuteNonQuery(command, commandType, Parameters, null);
        }

        public void ExecuteNonQuery(String command, CommandType commandType, SqlParameterCollection Parameters, SqlTransaction trans)
        {
            if ((i_cn == null) || (i_cn.State == ConnectionState.Closed))
            {
                closeDB();
                openDB();
            }

            Int16 step = 0;
            while ((step < 10) && (conn.State == ConnectionState.Connecting))
            {
                System.Threading.Thread.Sleep(100);
            }

            SqlCommand cmd = new SqlCommand(command, i_cn);
            cmd.CommandType = commandType;

            String debug = "";

            try
            {

                if (Parameters != null)
                {
                    cmd.Parameters.Clear();
                    foreach (SqlParameter par in Parameters)
                    {
                        debug += "DECLARE " + par.ParameterName + " " + par.SqlDbType.ToString();
                        if (par.Size > 0)
                            debug += "(" + par.Size + ")";
                        debug += Environment.NewLine;
                        debug += "SET " + par.ParameterName + " = " + par.Value + Environment.NewLine;

                        cmd.Parameters.Add(par.ParameterName, par.SqlDbType, par.Size).Value = par.Value;
                    }
                }

                if (trans != null)
                    cmd.Transaction = trans;

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Parameters != null) Parameters.Clear();
                Parameters = null;

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }


        public static DateTime DateGroup(DateTime date)
        {
            DateTime newDate = date;
            Int32 year = date.Year;
            Int32 day = date.Day;
            Int32 month = date.Month;
            Int32 hour = date.Hour;
            Int32 minute = date.Minute;
            Int32 second = 0;


            if (date.Second > 50)
            {
                second = 50;
            }
            else if (date.Second > 40)
            {
                second = 40;
            }
            else if (date.Second > 30)
            {
                second = 30;
            }
            else if (date.Second > 20)
            {
                second = 20;
            }
            else if (date.Second > 10)
            {
                second = 10;
            }
            else
            {
                second = 0;
            }


            return new DateTime(year, month, day, hour, minute, second);
        }

    }
}
