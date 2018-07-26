using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SafeTrend.Data.Update.SqlServer
{
   
    public class InitialCreateScript : IUpdateScript
    {
        public string Command
        {
            get
            {
                return @"

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'Events'))
                    BEGIN
                        DROP TABLE [Events];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'VU'))
                    BEGIN
                        DROP TABLE [VU];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'WebResult'))
                    BEGIN
                        DROP TABLE [WebResult];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'Messages'))
                    BEGIN
                        DROP TABLE [Messages];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'Optimization'))
                    BEGIN
                        DROP TABLE [Optimization];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'NonOptimization'))
                    BEGIN
                        DROP TABLE [NonOptimization];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'GzipOptimization'))
                    BEGIN
                        DROP TABLE [GzipOptimization];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'ZabbixMonitor'))
                    BEGIN
                        DROP TABLE [ZabbixMonitor];
                    END

                    IF (EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = 'ZabbixMonitorNetwork'))
                    BEGIN
                        DROP TABLE [ZabbixMonitorNetwork];
                    END

                    CREATE TABLE [Events] (
                        id bigint IDENTITY (1, 1) NOT NULL, 
                        test_id varchar(50) NOT NULL, 
                        date datetime not null, 
                        event_text varchar (2000) 
					);

                    CREATE TABLE [VU] (
                        date datetime not null DEFAULT GETDATE(), 
                        dateg datetime not null, 
                        pID bigint, 
                        testID varchar (50), 
                        virtualUsers bigint, 
                        connections bigint 
					);

                    CREATE TABLE [WebResult] (
                        date datetime not null DEFAULT GETDATE(), 
                        dateg datetime not null DEFAULT GETDATE(), 
                        pID bigint, 
                        testID varchar (50), 
                        uri varchar (3000), 
                        statusCode int, 
                        contentType varchar(300), 
                        bytesReceived bigint, 
                        time float, 
                        errorMessage varchar(3000) 
					);

                    CREATE TABLE [Messages] (
                        date datetime not null DEFAULT GETDATE(), 
                        pID bigint, 
                        testID varchar (50), 
                        title varchar(300), 
                        [text] varchar(max) 
					);

                    CREATE TABLE [Optimization] (
                        date datetime not null DEFAULT GETDATE(), 
                        pID bigint, 
                        testID varchar (50), 
                        uri varchar(3000), 
                        originalLength bigint, 
                        optimizedLength bigint 
					);

                    CREATE TABLE [NonOptimization] (
                        date datetime not null DEFAULT GETDATE(), 
                        pID bigint, 
                        testID varchar (50), 
                        uri varchar(3000), 
                        originalLength bigint, 
                        nonOptimizedLength bigint 
					);

                    CREATE TABLE [GzipOptimization] (
                        date datetime not null DEFAULT GETDATE(), 
                        pID bigint, 
                        testID varchar (50), 
                        uri varchar(3000), 
                        gzipLength bigint, 
                        contentLength bigint 
					);

                    CREATE TABLE [ZabbixMonitor] (
                        date datetime not null DEFAULT GETDATE(), 
                        dateg datetime not null DEFAULT GETDATE(), 
                        pID bigint, 
                        testID varchar (50), 
                        [host] varchar(300),
                        [key] varchar(300), 
                        [selector] varchar(300), 
                        [total_value] bigint DEFAULT 0,
                        [value] bigint DEFAULT 0
					);

                    CREATE TABLE [ZabbixMonitorNetwork] (
                        date datetime not null DEFAULT GETDATE(), 
                        dateg datetime not null DEFAULT GETDATE(), 
                        pID bigint, 
                        testID varchar (50), 
                        [host] varchar(300),
                        [interface] varchar(300), 
                        [in_value] bigint DEFAULT 0,
                        [out_value] bigint DEFAULT 0
					);

                    ";
            }
        }

        public string Precondition
        {
            get { return null; }
        }

        public Double Serial { get { return 1.1; } }

        public string Provider { get { return "System.Data.SqlClient"; } }
    }
}
