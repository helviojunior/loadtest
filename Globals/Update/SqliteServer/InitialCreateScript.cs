using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqliteServer
{
    public class InitialCreateScript : IUpdateScript
    {
        //
        public string Command
        {
            get
            {
                return @"

                    DROP TABLE IF EXISTS [Events];
                    DROP TABLE IF EXISTS [VU];
                    DROP TABLE IF EXISTS [WebResult];
                    DROP TABLE IF EXISTS [Messages];
                    DROP TABLE IF EXISTS [Optimization];
                    DROP TABLE IF EXISTS [NonOptimization];
                    DROP TABLE IF EXISTS [GzipOptimization];
                    DROP TABLE IF EXISTS [ZabbixMonitor];
	                    
                    CREATE TABLE [Events] (
                        id INTEGER PRIMARY KEY AUTOINCREMENT, 
                        test_id TEXT NOT NULL, 
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        event_text TEXT NULL
					);

                    CREATE TABLE [VU] (
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        dateg datetime not null  DEFAULT (datetime('now','localtime')), 
                        pID INTEGER, 
                        testID TEXT, 
                        virtualUsers INTEGER, 
                        connections INTEGER 
					);

                    CREATE TABLE [WebResult] (
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        dateg datetime not null  DEFAULT (datetime('now','localtime')), 
                        pID INTEGER, 
                        testID TEXT, 
                        uri TEXT, 
                        statusCode int, 
                        contentType TEXT, 
                        bytesReceived INTEGER, 
                        time float, 
                        errorMessage TEXT NULL
					);

                    CREATE TABLE [Messages] (
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        pID INTEGER, 
                        testID TEXT, 
                        title TEXT, 
                        [text] TEXT NULL
					);

                    CREATE TABLE [Optimization] (
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        pID INTEGER, 
                        testID TEXT, 
                        uri varchar(3000), 
                        originalLength INTEGER, 
                        optimizedLength INTEGER 
					);

                    CREATE TABLE [NonOptimization] (
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        pID INTEGER, 
                        testID TEXT, 
                        uri varchar(3000), 
                        originalLength INTEGER, 
                        nonOptimizedLength INTEGER 
					);

                    CREATE TABLE [GzipOptimization] (
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        pID INTEGER, 
                        testID TEXT, 
                        uri varchar(3000), 
                        gzipLength INTEGER, 
                        contentLength INTEGER 
					);

                    CREATE TABLE [ZabbixMonitor] (
                        date datetime not null  DEFAULT (datetime('now','localtime')), 
                        dateg datetime not null  DEFAULT (datetime('now','localtime')), 
                        pID INTEGER, 
                        testID TEXT, 
                        host TEXT, 
                        key TEXT, 
                        selector TEXT, 
                        total_value INTEGER,
                        value INTEGER
					);


                    ";
            }
        }

        public string Precondition
        {
            get { return null; }
        }

        public Double Serial { get { return 1.1; } }
        public string Provider { get { return "System.Data.SQLite"; } }
    }
}
