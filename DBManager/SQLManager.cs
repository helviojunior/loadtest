using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SqlClient;
//using System.Data.SqlServerCe;
using System.IO;
using System.Diagnostics;

namespace DBManager
{
    public class SQLManager : IDisposable
    {
        private String i_server;
        private String i_username;
        private String i_password;
        //private SqlConnection i_cn;
        //private Boolean i_Opened;
        private String i_ConnectionString;

        //public SqlConnection conn { get { return i_cn; } }
        //public Boolean isOpenned { get { return i_Opened; } }

        public SQLManager(String server, String username, String password)
        {
            i_server = server;
            i_username = username;
            i_password = password;
            i_ConnectionString = string.Format("Data Source={0};Initial Catalog=master;User Id={1};Password={2};", i_server, i_username, i_password);
        }

        public void CreateDB(String dbName)
        {
            CreateDB(dbName, false, false);
        }

        public void CreateDB(String dbName, Boolean forceReplace)
        {
            CreateDB(dbName, forceReplace, false);
        }

        public void CreateDB(String dbName, Boolean forceReplace, Boolean throwExceptions)
        {
            if (forceReplace)
            {
                DropDatabase(dbName);
            }

            SqlConnection conn = null;
            SqlCommand cmd = null;
            try
            {
                conn = new SqlConnection(i_ConnectionString);
                conn.Open();

                cmd = new SqlCommand("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'" + dbName + "') create database [" + dbName + "]", conn);
                cmd.CommandTimeout = 180000;
                cmd.ExecuteNonQuery();


                cmd = new SqlCommand("use [" + dbName + "]", conn);
                cmd.CommandTimeout = 180000;
                cmd.ExecuteNonQuery();

                CreateTables(conn, throwExceptions);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                if (throwExceptions)
                    throw ex;
            }
            finally
            {
                if (conn != null) conn.Close();
                if (conn != null) conn.Dispose();

                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
            
        }


        public void Dispose()
        {
            
            i_ConnectionString = null;

        }

        public void DropDatabase(String dbName)
        {
            SqlConnection conn = new SqlConnection(i_ConnectionString);
            conn.Open();

            SqlCommand cmd = null;
            try
            {

                //Finaliza as conexões com o DB que será excluido
                DataTable process = Select("SELECT CONVERT (VARCHAR(25), spid) AS spid FROM master..sysprocesses pr INNER JOIN master..sysdatabases db ON pr.dbid = db.dbid WHERE db.name = '" + dbName + "'", conn);
                foreach (DataRow dr in process.Rows)
                {
                    try
                    {
                        cmd = new SqlCommand("kill " + dr["spid"].ToString(), conn);
                        cmd.ExecuteNonQuery();
                    }
                    catch { }
                }

                //Exclui o DB
                cmd = new SqlCommand("IF EXISTS (SELECT name FROM sys.databases WHERE name = N'" + dbName + "') drop database  [" + dbName + "]", conn);
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                cmd = null;
            }


            if (conn != null) conn.Close();
            if (conn != null) conn.Dispose();
        }

        #region CreateTables

        private void CreateTables(SqlConnection conn1)
        {
            CreateTables(conn1, false);
        }

        private void CreateTables(SqlConnection conn1, Boolean throwExceptions)
        {
            CreateEventTable(conn1, throwExceptions);

            CreateVUTable(conn1, throwExceptions);
            CreateWebResultTable(conn1, throwExceptions);
            CreateMessagesTable(conn1, throwExceptions);
            CreateOptimizationTable(conn1, throwExceptions);
        }

        private void CreateTable(String SQL, SqlConnection conn)
        {
            CreateTable(SQL, conn, false);
        }

        private void CreateTable(String SQL, SqlConnection conn, Boolean throwExceptions)
        {
            SqlCommand cmd = null;
            try
            {
                cmd = new SqlCommand(SQL, conn);
                cmd.CommandTimeout = 180000;
                cmd.ExecuteNonQuery();

            }
            catch (SqlException sqlexception)
            {
                Debug.WriteLine(sqlexception.Message);

                if (throwExceptions)
                    throw sqlexception;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                if (throwExceptions)
                    throw ex;
            }
            finally
            {
                if (cmd != null) cmd.Dispose();
                cmd = null;
            }
        }

        private void CreateEventTable(SqlConnection conn1, Boolean throwExceptions)
        {
            string sql = "create table Events ("
            + "id bigint IDENTITY (1, 1) NOT NULL, "
            + "test_id varchar(50) NOT NULL, "
            + "date datetime not null, "
            + "event_text varchar (2000) )";

            CreateTable(sql, conn1, throwExceptions);
        }

        private void CreateVUTable(SqlConnection conn1, Boolean throwExceptions)
        {
            string sql = "create table VU ("
            + "date datetime not null, "
            + "dateg datetime not null, "
            + "pID bigint, "
            + "testID varchar (50), "
            + "virtualUsers bigint, "
            + "connections bigint )";

            CreateTable(sql, conn1, throwExceptions);
        }

        private void CreateWebResultTable(SqlConnection conn1, Boolean throwExceptions)
        {
            string sql = "create table WebResult ("
            + "date datetime not null, "
            + "dateg datetime not null, "
            + "pID bigint, "
            + "testID varchar (50), "
            + "uri varchar (3000), "
            + "statusCode int, "
            + "contentType varchar(300), "
            + "bytesReceived bigint, "
            + "time float, "
            + "errorMessage varchar(3000) )";

            CreateTable(sql, conn1, throwExceptions);

        }


        private void CreateMessagesTable(SqlConnection conn1, Boolean throwExceptions)
        {
            string sql = "create table Messages ("
            + "date datetime not null, "
            + "pID bigint, "
            + "testID varchar (50), "
            + "title varchar(300), "
            + "[text] varchar(max) )";

            CreateTable(sql, conn1, throwExceptions);

        }

        private void CreateOptimizationTable(SqlConnection conn1, Boolean throwExceptions)
        {
            string sql = "create table Optimization ("
            + "date datetime not null, "
            + "pID bigint, "
            + "testID varchar (50), "
            + "uri varchar(3000), "
            + "originalLength bigint, "
            + "optimizedLength bigint )";

            CreateTable(sql, conn1, throwExceptions);

        }


        #endregion CreateTables

        public DataTable Select(String SQL, SqlConnection conn)
        {
            try
            {
                SqlCommand select = new SqlCommand(SQL, conn);

                select.CommandType = CommandType.Text;

                SqlDataAdapter da = new SqlDataAdapter(select);
                DataSet ds = new DataSet();
                da.Fill(ds, "data");
                da.Dispose();

                DataTable tmp = ds.Tables["data"];

                select = null;
                ds = null;
                da = null;

                return tmp;
            }
            catch (SqlException dbEx)
            {
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }



        public DataTable selectAllFrom(String tableName, String filter, SqlConnection conn)
        {

            String SQL = "SELECT * " +
                         "FROM [" + tableName + "]";

            if ((filter != null) && (filter != ""))
                SQL += " WHERE " + filter;

            Debug.WriteLine(SQL);
            return Select(SQL, conn);
        }

    }
}
