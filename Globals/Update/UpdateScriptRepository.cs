using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace SafeTrend.Data.Update
{
    public static class UpdateScriptRepository
    {
        /// <summary>
        /// Creates the list of scripts that should be executed on app start. Ordering matters!
        /// </summary>
        public static IEnumerable<IUpdateScript> GetScriptsBySqlProviderName(DbConnectionString connectionString)
        {
            switch (connectionString.ProviderName.ToLower())
            {
                case "sqlite":
                case "system.data.sqlite":
                    return new List<IUpdateScript>
                    {
                        new SqliteServer.InitialCreateScript(),
                    };

                case "sqlclient":
                case "system.data.sqlclient":
                    return new List<IUpdateScript>
                    {
                        new SqlServer.InitialCreateScript(),
                    };

                default:
                    throw new NotImplementedException(string.Format("The provider '{0}' is not supported yet", connectionString.ProviderName));
            }
        }
    }
}