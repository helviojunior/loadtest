using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace SafeTrend.Data
{
    [Serializable()]
    public class DbConnectionString
    {
        private string name;
        private string connectionString;
        private string providerName;


        public string Name { get { return name; } }

        // Summary:
        //     Gets or sets the connection string.
        //
        // Returns:
        //     The string value assigned to the SafeTrend.Data.DbConnectionString.ConnectionString
        //     property.
        public string ConnectionString { get { return connectionString; } }


        //
        // Summary:
        //     Gets or sets the provider name property.
        //
        // Returns:
        //     Gets or sets the SafeTrend.Data.DbConnectionString.ProviderName
        //     property.
        public string ProviderName { get { return providerName; } }

        //
        // Summary:
        //     Initializes a new instance of a SafeTrend.Data.DbConnectionString
        //     class.
        //
        // Parameters:
        //   name:
        //     The connection string.
        //
        //   connectionString:
        //     The connection string.
        public DbConnectionString(ConnectionStringSettings connectionString)
        {
            this.name = connectionString.Name;
            this.connectionString = connectionString.ConnectionString;
            this.providerName = connectionString.ProviderName;
        }
        //
        // Summary:
        //     Initializes a new instance of a SafeTrend.Data.DbConnectionString
        //     object.
        //
        // Parameters:
        //   name:
        //     The name of the connection string.
        //
        //   connectionString:
        //     The connection string.
        //
        //   providerName:
        //     The name of the provider to use with the connection string.
        public DbConnectionString(string name, string connectionString, string providerName)
        {
            this.name = name;
            this.connectionString = connectionString;
            this.providerName = providerName;
        }

    }
}
