using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
namespace PlayerStats
{

	public class Database
	{
		private readonly IDbConnection _db;

		internal QueryResult QueryReader(string query, params object[] args)
		{
			return _db.QueryReader(query, args);
		}

		internal int Query(string query, params object[] args)
		{
            
			return _db.Query(query, args);
		}

		internal void EnsureExists(SqlTable table)
		{
			var creator = new SqlTableCreator(_db,
				_db.GetSqlType() == SqlType.Sqlite
					? (IQueryBuilder) new SqliteQueryCreator()
					: new MysqlQueryCreator());
 			bool s = creator.EnsureTableStructure(table);
 		}

		internal void EnsureExists(params SqlTable[] tables)
		{
			foreach (var table in tables)
				EnsureExists(table);
		}

        internal void truncateData()
        {
                        int result = Query("delete from Stats");
                    }
 
		private Database(IDbConnection db)
		{
			_db = db;
		}

		public static Database InitDb(string name)
		{
			IDbConnection idb;

			if (TShock.Config.StorageType.ToLower() == "sqlite")
				idb =
					new SqliteConnection(string.Format("uri=file://{0},Version=3",
						Path.Combine(TShock.SavePath, name + ".sqlite")));

			else if (TShock.Config.StorageType.ToLower() == "mysql")
			{
				try
				{
					var host = TShock.Config.MySqlHost.Split(':');
					idb = new MySqlConnection
					{
						ConnectionString = String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
							host[0],
							host.Length == 1 ? "3306" : host[1],
							TShock.Config.MySqlDbName,
							TShock.Config.MySqlUsername,
							TShock.Config.MySqlPassword
							)
					};
				}
				catch (MySqlException x)
				{
					TShock.Log.Error(x.ToString());
					throw new Exception("MySQL not setup correctly.");
				}
			}
			else
				throw new Exception("Invalid storage type.");

			var db = new Database(idb);
			return db;
		}
	}
    public class ProfileList
    {
        public int ID { get; set; }
        public string TimeStamp { get; set; }
        public string Inventory { get; set; }

        public ProfileList(int id, string timeStamp, string inventory)
        {
            ID = id;
            TimeStamp = timeStamp;
            Inventory = inventory;
        }

        public ProfileList()
        {
            ID = 0;
            TimeStamp = "";
            Inventory = "";
        }
    }
    public class PlayerStatsList
    {
        public int V1 { get; set; }
        public string V2 { get; set; }
        public int V3 { get; set; }
        public int V4 { get; set; }
        public int V5 { get; set; }

        public PlayerStatsList(int v1, string v2, int v3, int v4, int v5)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
            V4 = v4;
            V5 = v5;
        }

        public PlayerStatsList()
        {
            V1 = 0;
            V2 = "";
            V3 = 0;
            V4 = 0;
            V5 = 0;
        }
    }
}