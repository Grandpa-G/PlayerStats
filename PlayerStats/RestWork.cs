using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;

using Terraria;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;
using TerrariaApi;
using TerrariaApi.Server;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using Rests;

namespace PlayerStats
{
    class RestWork
    {
        public static RestObject getVersion(RestRequestArgs args)
        {
            String dbType;
            if (TShock.DB.GetSqlType() == SqlType.Sqlite)
                dbType = "SQLite";
            else
                dbType = "MySQL";
            return new RestObject()
			{
				 { "version", Assembly.GetExecutingAssembly().GetName().Version.ToString() },
				{ "db", dbType }
			};

        }
 
        public static RestObject getPlayerStats(RestRequestArgs args)
        {

            string searchString = Convert.ToString(args.Parameters["search"]);
            if (searchString == null)
                searchString = "";
            PlayerStatsList rec = null;
            String sql;
            List<PlayerStatsList> playerStatsList = new List<PlayerStatsList>();

            try
            {
                sql = "SELECT * FROM PlayerStats " + searchString;
                using (var reader = PlayerStats.playerDb.QueryReader(searchString))
                {
                    while (reader.Read())
                    {
                        rec = new PlayerStatsList(reader.Get<int>("V1"), reader.Get<string>("V2"), reader.Get<int>("V3"), reader.Get<int>("V4"), reader.Get<int>("V5"));
                        playerStatsList.Add(rec);
                    }
                }

                return new RestObject() { { "Rows", playerStatsList }, { "version", Assembly.GetExecutingAssembly().GetName().Version.ToString() } };

            }
            catch (Exception ex)
            {
                TShock.Log.Error(ex.ToString());
                Console.WriteLine(ex.StackTrace);
            }
            return null;

        }
    }
}
