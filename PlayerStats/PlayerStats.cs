using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel;
using System.Timers;


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
    [ApiVersion(1, 17)]
    public class PlayerStats : TerrariaPlugin
    {
        internal static Database tshock;

        private Timer playerTimer;
        internal static Database playerDb;
        private Config playerConfig;
        internal static string dbPrefix;
        private string[] playerSessionId;
        private string playerTimeInterval;

        private readonly List<string> joinedIPs = new List<string>();

        public override string Name
        {
            get { return "PlayerStats"; }
        }
        public override string Author
        {
            get { return "Granpa-G"; }
        }
        public override string Description
        {
            get { return "Provides a profile information about a TShock server player activity."; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
        public PlayerStats(Main game)
            : base(game)
        {
            Order = 1;
        }
        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);

            TShock.RestApi.Register(new SecureRestCommand("/PlayerStats/getPlayerStats", RestWork.getPlayerStats, "PlayerStats.allow"));
            TShock.RestApi.Register(new SecureRestCommand("/PlayerStats/version", RestWork.getVersion, "PlayerStats.allow"));

        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null)
                return;

            if (playerSessionId[player.Index].Length == 0)
                return;

            Console.WriteLine(playerDb.Query("UPDATE " + dbPrefix + "PlayerLog set Logout=@1 where SessionId= @0 and Logout is null", playerSessionId[player.Index], DateTime.Now.ToString("G")));
            playerSessionId[player.Index] = "";

        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnGameInitialize);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
            }
            base.Dispose(disposing);
        }

        private void OnGameInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("PlayerStats.allow", setupProfile, "playerstats"));
            Commands.ChatCommands.Add(new Command("PlayerStats.allow", setupProfile, "ps"));

            playerSessionId = new string[TShock.Config.MaxSlots];

            var path = Path.Combine(TShock.SavePath, "PlayerStats.json");
            (playerConfig = Config.Read(path)).Write(path);

            playerDb = Database.InitDb("PlayerProfileStats");
            dbPrefix = "PlayerProfileStats.";
            if (TShock.Config.StorageType.ToLower() == "sqlite")
                dbPrefix = "";
            playerTimer = new Timer(playerConfig.interval * 1000 * 60); // in minutes
            playerTimer.Elapsed += UpdateTimerOnElapsed;

            try
            {
                playerDb.QueryReader("SELECT count(*) FROM " + dbPrefix + "PlayerStats ");
            }
            catch (Exception)
            {
                if (TShock.Config.StorageType.ToLower() == "sqlite")
                {
                    Console.Write("Setting up SQLite tables...");
                    playerDb.Query("CREATE TABLE PlayerStats (ID INTEGER PRIMARY KEY AUTOINCREMENT, Timestamp DATETIME, UniquePlayers INTEGER, ActivePlayers INTEGER, MaxPlayers INTEGER)");
                    playerDb.Query("CREATE INDEX idxTimestamp ON PlayerStats (Timestamp)");
                    Console.Write("working...");
                }
                else
                {
                    Console.WriteLine("Setting up mySQL tables...");
                    playerDb.Query("CREATE DATABASE PlayerProfileStats");
                    playerDb.Query("CREATE TABLE " + dbPrefix + "PlayerStats (ID INT NOT NULL AUTO_INCREMENT, Timestamp DATETIME, UniquePlayers INTEGER, ActivePlayers INTEGER, MaxPlayers INTEGER, PRIMARY KEY (ID))");
                    playerDb.Query("CREATE INDEX idxTimestamp ON " + dbPrefix + "PlayerStats (Timestamp)");
                    Console.Write("working...");
                }
                Console.WriteLine("done");
            }

            try
            {
                playerDb.QueryReader("SELECT count(*) FROM " + dbPrefix + "PlayerLog ");
            }
            catch (Exception)
            {
                if (TShock.Config.StorageType.ToLower() == "sqlite")
                {
                    Console.Write("Setting up SQLite tables...");
                    playerDb.Query("CREATE TABLE PlayerLog (ID INTEGER PRIMARY KEY AUTOINCREMENT,  SessionId TEXT, Name TEXT, UUID TEXT, Login DATETIME, Logout DATETIME)");
                    playerDb.Query("CREATE INDEX idxName ON PlayerLog (Name)");
                    playerDb.Query("CREATE INDEX idxSessionId ON PlayerLog (SessionId)");
                    playerDb.Query("CREATE INDEX idxLogin ON PlayerLog (Login)");
                    playerDb.Query("CREATE INDEX idxLogout ON PlayerLog (Logout)");
                    Console.Write("working...");
                }
                else
                {
                    Console.Write("Setting up mySQL tables...");
                    playerDb.Query("CREATE TABLE " + dbPrefix + "PlayerLog (ID INT NOT NULL AUTO_INCREMENT,  SessionId TEXT, Name TEXT, UUID TEXT, Login DATETIME, Logout DATETIME, PRIMARY KEY (ID))");
                    playerDb.Query("CREATE INDEX idxName ON " + dbPrefix + "PlayerLog (Name(15))");
                    playerDb.Query("CREATE INDEX idxSessionId ON " + dbPrefix + "PlayerLog (SessionId (16))");
                    playerDb.Query("CREATE INDEX idxLogin ON " + dbPrefix + "PlayerLog (Login)");
                    playerDb.Query("CREATE INDEX idxLogout ON " + dbPrefix + "PlayerLog (Logout)");
                    Console.Write("working...");
                }
                Console.WriteLine("done");
            }
            playerTimer.Start();
            playerTimeInterval = DateTime.Now.ToString("G");
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player == null)
            {
                return;
            }

            playerSessionId[player.Index] = Guid.NewGuid().ToString("N");
            playerDb.Query("INSERT INTO " + dbPrefix + "PlayerLog (SessionId, Name, UUID, Login, Logout) VALUES (@0, @1, @2, @3, null)", playerSessionId[player.Index], player.Name, player.UUID, DateTime.Now.ToString("G"));
        }

        private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            TShockAPI.TShock.Log.ConsoleInfo(" Player Stats: {0} players ({1})", TShock.Utils.ActivePlayers(), TShock.Config.MaxSlots);
            playerDb.Query(
                "INSERT INTO " + dbPrefix + "PlayerStats (Timestamp, UniquePlayers, ActivePlayers, MaxPlayers) VALUES (@0, @1, @2, @3)",
                DateTime.Now.ToString("G"), 0, TShock.Utils.ActivePlayers(), TShock.Config.MaxSlots);

            joinedIPs.Clear();
            playerTimeInterval = DateTime.Now.ToString("G");
        }

        private static Timer aTimer;
        private static Timer profileTimer;
        private void setupProfile(CommandArgs args)
        {
            bool verbose = false;
            bool clearData = false;
            bool help = false;
            InventoryProfileArguments arguments = new InventoryProfileArguments(args.Parameters.ToArray());
            if (arguments.Contains("-help"))
                help = true;
            if (arguments.Contains("-clear"))
                clearData = true;
            if (arguments.Contains("-v"))
                verbose = true;

            if (help)
            {
                args.Player.SendMessage("Syntax: /iprofile [-help | -on | -off | -clear] ", Color.Red);
                args.Player.SendMessage("Flags: ", Color.LightSalmon);
                args.Player.SendMessage("   -help     this information", Color.LightSalmon);
                return;
            }

            if (arguments.Contains("-l"))
            {
                args.Player.SendSuccessMessage("Timer for player stats set at {0} minutes.", playerTimer.Interval / (60.0 * 1000.0));
            }

            if (arguments.Contains("-r"))
            {
                var path = Path.Combine(TShock.SavePath, "PlayerStats.json");
                playerConfig = Config.Read(path);
                playerTimer = new Timer(playerConfig.interval * 1000 * 60); // in minutes
                args.Player.SendSuccessMessage("Timer reloaded for player stats set at {0} minutes.", playerTimer.Interval / (60.0 * 1000.0));
            }

            if (arguments.Contains("-init"))
            {
                DateTime dt = DateTime.Now; // or something like this
                DateTime dtoff = DateTime.Now; // or something like this
                Random rnd = new Random();
                Random rnds = new Random();

                string[] n = new string[] { "Fred", "Tom", "Joe", "master", "John", "Paul", "Mary", "Nerdy", "Derpy", "HoboJoe", "CatGoturTongue", "Jinx" };
                int r = rnd.Next(1, 3 * 60);
                int s = rnds.Next(1, 60 * 24);
                playerDb.Query("DELETE from " + dbPrefix + "PlayerLog ");
                for (int j = 0; j < n.Length; j++)
                {
                    Console.WriteLine(n[j]);
                    for (int i = 0; i < 50; i++)
                    {
                        dt = DateTime.Now.AddHours(-s);
                        dtoff = dt.AddMinutes(r * 3);
                        playerDb.Query("INSERT INTO " + dbPrefix + "PlayerLog (SessionId, Name, UUID, Login, Logout) VALUES (@0, @1, @2, @3, @4)",
                            Guid.NewGuid().ToString("N"), n[j], "xxxxxxxxxxxxx", dt, dtoff);
                        r = rnd.Next(1, 100 * 60);
                        s = rnds.Next(1, 60 * 24);
                    }
                }
                dt = DateTime.Now;
                rnd = new Random();

                r = rnd.Next(1, 30);
                playerDb.Query("DELETE from " + dbPrefix + "PlayerStats ");
                for (int i = 0; i < 24 * 45; i++)
                {
                    dt = DateTime.Now.AddHours(-i);
                    playerDb.Query("INSERT INTO " + dbPrefix + "PlayerStats (Timestamp, UniquePlayers, ActivePlayers, MaxPlayers) VALUES (@0, @1, @2, @3)",
                       dt, 0, r, 30);
                    r = rnd.Next(1, 30);
                }
            }

            if (clearData)
            {
                Console.Write("Clearing..");
                playerDb.Query("DELETE from " + dbPrefix + "PlayerStats ");
                Console.Write(".");
                playerDb.Query("DELETE from " + dbPrefix + "PlayerLog ");
                Console.WriteLine("Done");
            }
        }

        #region utility
        public static int ITEMOFFSET = 48;
        public static int MAXITEMS = 2748 + ITEMOFFSET + 1;
        public static int MAXITEMSPREFIX = 83 + 1;
        public static int MAXPREFIX = 86;

        private static Item[] itemList = new Item[MAXITEMS];
        private static int[] prefixList = new int[MAXPREFIX];

        private void loadItemNames()
        {
            int counter = 0;
            string[] linearray;
            string name;
            int netId;
            int stackSize;
            int prefix;
            for (int i = 0; i < itemList.Length; i++)
                itemList[i] = new Item();

            for (int i = 0; i < prefixList.Length; i++)
                prefixList[i] = 0;
            /*
            Assembly assem = Assembly.GetExecutingAssembly();
            counter = 0;
            string[] line = InventoryProfile.Properties.Resources.itemlist.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < line.Length; i++)
            {
                linearray = line[i].Split('`');
                netId = Int32.Parse(linearray[0].Trim());
                name = linearray[1].Trim();
                stackSize = Int32.Parse(linearray[3].Trim());
                prefix = Int32.Parse(linearray[2].Trim());
                itemList[counter] = new Item(name, netId, stackSize, prefix);
                 counter++;
            }
            counter = 0;

            line = InventoryProfile.Properties.Resources.prefixlist.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < line.Length; i++)
            {
                linearray = line[i].Split(':');
                name = linearray[0].Trim();
                prefix = Int32.Parse(linearray[1].Trim());
                prefixList[counter] = new Prefixs(name, prefix);
                 counter++;
            }
            */
        }

        #endregion
    }
    #region application specific commands
    public class InventoryProfileArguments : InputArguments
    {
        public string Help
        {
            get { return GetValue("-help"); }
        }


        public InventoryProfileArguments(string[] args)
            : base(args)
        {
        }

        protected bool GetBoolValue(string key)
        {
            string adjustedKey;
            if (ContainsKey(key, out adjustedKey))
            {
                bool res;
                bool.TryParse(_parsedArguments[adjustedKey], out res);
                return res;
            }
            return false;
        }
    }
    #endregion

}
