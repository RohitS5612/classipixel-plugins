//reference System.Core.dll

/* NOTE:
    - "Paintball" is the gamemode name - modify if needed
    - "PB" is the shortcut - modify if needed
    
    ^ Easiest way is CTRL + H in most text/code editors.
    
    - To add maps, you will need to type /pb add.
    - This is a template-based plugin, feel free to modify the config section or add your own behaviour.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

using MCGalaxy.Commands;
using MCGalaxy.Commands.Fun;
using MCGalaxy.Config;
using MCGalaxy.Events.PlayerEvents;
using MCGalaxy.Events.ServerEvents;
using MCGalaxy.Maths;
using MCGalaxy.Network;
using MCGalaxy.SQL;

using BlockID = System.UInt16;

namespace MCGalaxy.Games
{
    // Stores per-map configuration like spawn points
    // Each map can have its own spawn location saved in plugins/Paintball/maps/<mapname>.config
    public class PaintballMapConfig
    {
        [ConfigVec3("paintball-spawn", null)]
        public Vec3U16 Spawn; // Spawn coordinates for players

        static string Path(string map) { return "./plugins/Paintball/maps/" + map + ".config"; }
        static ConfigElement[] cfg;

        public void SetDefaults(Level lvl)
        {
            Spawn.X = (ushort)(lvl.Width / 2);
            Spawn.Y = (ushort)(lvl.Height / 2 + 1);
            Spawn.Z = (ushort)(lvl.Length / 2);
        }

        public void Load(string map)
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(PaintballMapConfig));
            ConfigElement.ParseFile(cfg, Path(map), this);
        }

        public void Save(string map)
        {
            if (cfg == null) cfg = ConfigElement.GetAll(typeof(PaintballMapConfig));
            using (StreamWriter w = new StreamWriter(Path(map))) {
                w.WriteLine("# Paintball map config for " + map);
                w.WriteLine("paintball-spawn = " + Spawn.X + " " + Spawn.Y + " " + Spawn.Z);
            }
        }
    }


    // Stores per-player game data during a round
    // Tracks tokens earned and kills made
    public sealed class PaintballData
    {
        public int Tokens = 0; // Tokens earned throughout the round
        public int Kills = 0; // Total kills
    }

    // Main plugin class - handles loading, unloading, and initialization
    // This is the entry point that MCGalaxy calls when loading the plugin
    public sealed class PaintballPlugin : Plugin
    {
        public override string creator { get { return "RohitS5612"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string name { get { return "Paintball"; } }

        public static ChatToken PaintballToken;

        static string TokenPaintball(Player p)
        {
            Player[] players = PlayerInfo.Online.Items;
            int count = 0;

            foreach (Player pl in players)
            {
                if (!PaintballGame.Instance.Running) return "0";
                if (pl.level.name == PaintballGame.Instance.Map.name) count++;
            }

            return count.ToString();
        }

        // Table structure for custom statistics
        ColumnDesc[] createDatabase = new ColumnDesc[] {
            new ColumnDesc("Name", ColumnType.VarChar, 16),
            new ColumnDesc("RoundsPlayed", ColumnType.Int32),
            new ColumnDesc("RoundsWon", ColumnType.Int32),
            new ColumnDesc("MoneyEarned", ColumnType.Int32),
            new ColumnDesc("Kills", ColumnType.Int32),
            // Add any other columns here
        };

        public override void Load(bool startup)
        {
            // Create necessary directories
            Directory.CreateDirectory("plugins/Paintball");
            Directory.CreateDirectory("plugins/Paintball/maps");
            
            // Add token into the server
            PaintballToken = new ChatToken("$paintball", "Paintball", TokenPaintball);
            ChatTokens.Standard.Add(PaintballToken);

            OnConfigUpdatedEvent.Register(OnConfigUpdated, Priority.Low);

            PaintballGame.Instance.Config.Path = "plugins/Paintball/game.properties";
            OnConfigUpdated();

            if (PaintballGame.customStats) Database.CreateTable("Stats_Paintball", createDatabase);

            Command.Register(new CmdPaintball());

            RoundsGame game = PaintballGame.Instance;
            game.GetConfig().Load();
            if (!game.Running) game.AutoStart();
        }

        public override void Unload(bool shutdown)
        {
            ChatTokens.Standard.Remove(PaintballToken);

            OnConfigUpdatedEvent.Unregister(OnConfigUpdated);

            Command.Unregister(Command.Find("Paintball"));

            RoundsGame game = PaintballGame.Instance;
            if (game.Running) game.End();
        }

        void OnConfigUpdated()
        {
            PaintballGame.Instance.Config.Load();
        }
    }

    // Configuration for the rounds game system
    // Handles game.properties file loading and autostart settings
    public sealed class PaintballConfig : RoundsGameConfig
    {
        public override bool AllowAutoload { get { return true; } }
        protected override string GameName { get { return "Paintball"; } }
    }

    // Main game logic class - handles rounds, player tracking, events
    // Extends RoundsGame to get automatic round management functionality
    public sealed partial class PaintballGame : RoundsGame
    {
        public VolatileArray<Player> Alive = new VolatileArray<Player>(); // Thread-safe list of alive players

        public static PaintballGame Instance = new PaintballGame();
        public PaintballGame() { }

        public PaintballConfig Config = new PaintballConfig();
        public override RoundsGameConfig GetConfig() { return Config; }

        public override string GameName { get { return "Paintball"; } }
        public int Interval = 1000;
        public PaintballMapConfig cfg = new PaintballMapConfig();

        protected override string WelcomeMessage
        {
            get { return ""; } // Message shown to players when connecting
        }

        // =========================================== CONFIG =======================================
        // Modify these values to change game behavior

        public static bool pvp = true; // Whether or not to allow players to fight each other
        public static bool buildable = false; // Whether or not to make the map buildable on round start
        public static bool deletable = false; // Whether or not to make the map deletable on round start
        public static bool altDetection = false; // Whether or not to give rewards to players if they share an IP with any players online
        public static bool customStats = true; // Whether or not the plugin should implement custom statistics for rounds played, wins and money earned

        public static int winReward = 10; // Amount given to the player who wins
        public static int killReward = 1; // Amount given to players for every kill (incremental)
        public static int participationReward = 1; // Amount given to players for playing a round
        public static int countdownTimer = 30; // Time (in seconds) to check for players before starting a round

        // ============================================ GAME =======================================
        // Core game methods: starting, stopping, player management
        public override void UpdateMapConfig()
        {
            cfg = new PaintballMapConfig();
            cfg.SetDefaults(Map);
            cfg.Load(Map.name);
        }

        protected override List<Player> GetPlayers()
        {
            return Map.getPlayers();
        }

        public override void OutputStatus(Player p)
        {
            Player[] alive = Alive.Items;
            p.Message("Alive players: " + alive.Join(pl => pl.ColoredName));
        }

        public override void Start(Player p, string map, int rounds)
        {
            // Starts on current map by default
            if (!p.IsSuper && map.Length == 0) map = p.level.name;
            base.Start(p, map, rounds);
        }

        protected override void StartGame() { Config.Load(); }

        protected override void EndGame()
        {
            if (RoundInProgress) EndRound(null);
            Alive.Clear();
        }

        public override void PlayerLeftGame(Player p)
        {
            p.Extras.Remove("PAINTBALL_HIDE_HUD");
            // "kill" player if they leave server or change map
            if (!Alive.Contains(p)) return;
            Alive.Remove(p);
            UpdatePlayersLeft();
        }

        protected override string FormatStatus1(Player p)
        {
            return RoundInProgress ? "&b" + Alive.Count + " &Splayers left" : "";
        }

        // ============================================ PLUGIN =======================================
        // Event handlers: chat, spawning, joining levels
        protected override void HookEventHandlers()
        {
            OnPlayerSpawningEvent.Register(HandlePlayerSpawning, Priority.High);
            OnJoinedLevelEvent.Register(HandleOnJoinedLevel, Priority.High);
            OnPlayerChatEvent.Register(HandlePlayerChat, Priority.High);

            base.HookEventHandlers();
        }

        protected override void UnhookEventHandlers()
        {
            OnPlayerSpawningEvent.Unregister(HandlePlayerSpawning);
            OnJoinedLevelEvent.Unregister(HandleOnJoinedLevel);
            OnPlayerChatEvent.Unregister(HandlePlayerChat);

            base.UnhookEventHandlers();
        }

        // Checks if player votes for a map when voting in progress "1, 2, 3"
        void HandlePlayerChat(Player p, string message)
        {
            if (p.level != PaintballGame.Instance.Map) return;
            if (Picker.HandlesMessage(p, message)) { p.cancelchat = true; return; }
        }

        // This event is called when a player is killed
        void HandlePlayerSpawning(Player p, ref Position pos, ref byte yaw, ref byte pitch, bool respawning)
        {
            if (!respawning || !Alive.Contains(p)) return;
            if (p.Game.Referee) return;
            if (p.level != Map) return;

            Alive.Remove(p); // Remove them from the alive list
            UpdatePlayersLeft();
            p.Game.Referee = true; // This allows them to fly and noclip when they die
            p.Send(Packet.HackControl(true, true, true, true, true, -1)); // ^

            Entities.GlobalDespawn(p, true); // Remove from tab list
            Server.hidden.Add(p.name);
        }

        // We use this event for resetting everything and preparing for the next map
        void HandleOnJoinedLevel(Player p, Level prevLevel, Level level, ref bool announce)
        {
            p.Extras.Remove("PAINTBALL_INDEX");
            HandleJoinedCommon(p, prevLevel, level, ref announce);

            Entities.GlobalSpawn(p, true); // Adds player back to the tab list

            if (level == Map)
            {
                // Revert back to -hax
                p.Game.Referee = false;
                p.Send(Packet.Motd(p, "-hax -push"));
                p.invincible = true;

                if (Running)
                {
                    if (RoundInProgress)
                    {
                        // Force spectator mode if they join late
                        p.Game.Referee = true;
                        p.Send(Packet.HackControl(true, true, true, true, true, -1));
                        p.Message("You joined in the middle of the round so you are now a spectator.");
                        return;
                    }

                    else
                    {
                        List<Player> players = level.getPlayers();

                        foreach (Player pl in players)
                        {
                            Server.hidden.Remove(pl.name);
                            pl.Extras.Remove("PAINTBALL_INDEX");
                        }
                    }
                }
            }

            else
            {
                p.Game.Referee = false;
                p.invincible = false;
            }
        }

        const string paintballExtrasKey = "MCG_PAINTBALL_DATA";
        public static PaintballData Get(Player p)
        {
            PaintballData data = TryGet(p);
            if (data != null) return data;
            data = new PaintballData();

            p.Extras[paintballExtrasKey] = data;
            return data;
        }

        static PaintballData TryGet(Player p)
        {
            object data; p.Extras.TryGet(paintballExtrasKey, out data); return (PaintballData)data;
        }

        // ============================================ ROUND =======================================
        // Round logic: countdown, player setup, game loop, winner determination
        protected override void DoRound()
        {
            if (!Running) return;
            PaintballGame.Instance.Map.Config.Deletable = false;
            PaintballGame.Instance.Map.Config.Buildable = false;
            Map.UpdateBlockPermissions();

            DoRoundCountdown(countdownTimer); // Countdown to check if there are enough players before starting round
            if (!Running) return;

            UpdateMapConfig();
            if (!Running) return;

            List<Player> players = Map.getPlayers();

            foreach (Player pl in players)
            {
                Alive.Add(pl); // Adds them to the alive list
            }

            if (!Running) return;

            RoundInProgress = true;

            foreach (Player pl in players)
            {
                if (pl.level == Map)
                {
                    pl.Extras.Remove("PAINTBALL_HIDE_HUD");

                    if (pl.Game.Referee) continue;

                    Alive.Add(pl);

                    if (pvp) pl.Extras["PVP_CAN_KILL"] = true;
                    pl.Extras.Remove("PAINTBALL_INDEX");

                    pl.invincible = false;

                    pl.Send(Packet.Motd(pl, "-hax -push"));
                    pl.Extras["MOTD"] = "-hax -push";

                    if (PaintballGame.customStats)
                    {
                        // Custom statistics
                        List<string[]> rows = Database.GetRows("Stats_Paintball", "*", "WHERE Name=@0", pl.truename);

                        if (rows.Count == 0)
                        {
                            Database.AddRow("Stats_Paintball", "Name, RoundsPlayed, RoundsWon, MoneyEarned, Kills", pl.truename, 1, 0, 0, 0);
                        }

                        else
                        {
                            int played = int.Parse(rows[0][1]);
                            Database.UpdateRows("Stats_Paintball", "RoundsPlayed=@1", "WHERE NAME=@0", pl.truename, played + 1);
                        }
                    }
                }
            }

            // Allow modifying of the map

            if (buildable) PaintballGame.Instance.Map.Config.Buildable = true;
            if (deletable) PaintballGame.Instance.Map.Config.Deletable = true;
            Map.UpdateBlockPermissions();

            UpdateAllStatus1();

            while (RoundInProgress && Alive.Count > 0)
            {
                Thread.Sleep(Interval);

                Level map = Map;
            }
        }

        void UpdatePlayersLeft()
        {
            if (!RoundInProgress) return;
            Player[] alive = Alive.Items;
            List<Player> players = Map.getPlayers();

            if (alive.Length == 1)
            {
                // Prevent players from fighting after round ends
                foreach (Player pl in players) pl.Extras["PVP_CAN_KILL"] = false;

                // Nobody left except winner
                Map.Message(alive[0].ColoredName + " &Sis the winner!");

                PaintballGame.Instance.Map.Config.Deletable = false;
                PaintballGame.Instance.Map.Config.Buildable = false;
                Map.UpdateBlockPermissions();

                EndRound(alive[0]);
            }
            else
            {
                // Show alive player count
                Map.Message("&b" + alive.Length + " &Splayers left!");
            }
            UpdateAllStatus1();
        }

        public override void EndRound() { EndRound(null); }
        void EndRound(Player winner)
        {
            RoundInProgress = false;
            Alive.Clear();

            // Temporary IP storage for alt detection
            List<string> uniqueIPs = new List<string>();

            Player[] players = PlayerInfo.Online.Items;

            foreach (Player pl in players)
            {
                if (pl.level != Instance.Map) continue;
                pl.Extras["PAINTBALL_HIDE_HUD"] = true;

                if (customStats && pl == winner)
                {
                    // Custom statistics
                    List<string[]> rows = Database.GetRows("Stats_Paintball", "*", "WHERE Name=@0", winner.truename);

                    if (rows.Count == 0)
                    {
                        Database.AddRow("Stats_Paintball", "Name, RoundsPlayed, RoundsWon, MoneyEarned, Kills", winner.truename, 1, 1, 0, 0);
                    }

                    else
                    {
                        int wins = int.Parse(rows[0][2]);
                        Database.UpdateRows("Stats_Paintball", "RoundsWon=@1", "WHERE NAME=@0", winner.truename, wins + 1);
                    }
                }

                PaintballData data = Get(pl);

                if (altDetection)
                {
                    if (uniqueIPs.Contains(pl.ip))
                    {
                        pl.Message("&7You have been detected as playing with an alt. As such, you have not earned any tokens this round.");
                        continue;
                    }

                    uniqueIPs.Add(pl.ip);
                }

                if (participationReward > 0) data.Tokens += participationReward;

                if (killReward > 0)
                {
                    if (data.Kills > 0)
                    {
                        data.Tokens += data.Kills * killReward;
                        pl.Message(data.Kills + " &7kills = &b" + data.Kills + " &f↕");
                    }
                }

                if (pl == winner)
                {
                    winner.Message("&dCongratulations, you won this round of Paintball!");
                    data.Tokens += winReward;
                }

                if (customStats)
                {
                    // Custom statistics
                    List<string[]> rows = Database.GetRows("Stats_Paintball", "*", "WHERE Name=@0", pl.truename);

                    if (rows.Count == 0)
                    {
                        Database.AddRow("Stats_Paintball", "Name, RoundsPlayed, RoundsWon, MoneyEarned, Kills", pl.truename, 0, 0, data.Tokens, 0);
                    }

                    else
                    {
                        int winnings = int.Parse(rows[0][3]);
                        Database.UpdateRows("Stats_Paintball", "MoneyEarned=@1", "WHERE NAME=@0", pl.truename, winnings + data.Tokens);
                    }
                }

                pl.SetMoney(pl.money + data.Tokens);
            }

            if (altDetection) uniqueIPs.Clear();

            UpdateAllStatus1();

            BufferedBlockSender bulk = new BufferedBlockSender(Map);

            bulk.Flush();
        }

        // ============================================ STATS =======================================
    }

    // Command handler for /paintball and /pb
    // Provides: start, stop, end, add, remove, status, go, set spawn
    public sealed class CmdPaintball : RoundsGameCmd
    {
        public override string name { get { return "Paintball"; } }
        public override string shortcut { get { return "pb"; } }
        protected override RoundsGame Game { get { return PaintballGame.Instance; } }
        public override CommandPerm[] ExtraPerms
        {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can manage Paintball") }; }
        }

        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();
            if (args.Length > 0 && args[0].CaselessEq("clear"))
            {
                HandleClear(p, args);
                return;
            }
            base.Use(p, message, data);
        }

        void HandleClear(Player p, string[] args)
        {
            if (args.Length < 2 || !args[1].Equals("--confirm"))
            {
                p.Message("&cAre you sure you want to clear all Paintball maps from the list?");
                p.Message("&cUse: &f/pb clear --confirm &cto proceed.");
                return;
            }

            int count = Game.GetConfig().Maps.Count;
            Game.GetConfig().Maps.Clear();
            Game.GetConfig().Save();
            p.Message("&aCleared all {0} Paintball map(s) from the list.", count);
        }

        protected override void HandleStart(Player p, RoundsGame game, string[] args)
        {
            if (game.Running) { p.Message("{0} is already running", game.GameName); return; }

            int interval = 150;
            if (args.Length > 1 && !CommandParser.GetInt(p, args[1], "Delay", ref interval, 1, 1000)) return;

            ((PaintballGame)game).Interval = interval;
            game.Start(p, "", int.MaxValue);
        }

        protected override void HandleSet(Player p, RoundsGame game, string[] args)
        {
            if (args.Length < 2) { Help(p, "set"); return; }
            string prop = args[1];

            if (prop.CaselessEq("spawn"))
            {
                PaintballMapConfig cfg = RetrieveConfig(p);
                cfg.Spawn = (Vec3U16)p.Pos.FeetBlockCoords;
                p.Message("Set spawn pos to: &b{0}", cfg.Spawn);
                UpdateConfig(p, cfg);
                return;
            }

            if (args.Length < 3) { Help(p, "set"); }
        }

        static PaintballMapConfig RetrieveConfig(Player p)
        {
            PaintballMapConfig cfg = new PaintballMapConfig();
            cfg.SetDefaults(p.level);
            cfg.Load(p.level.name);
            return cfg;
        }

        static void UpdateConfig(Player p, PaintballMapConfig cfg)
        {
            if (!Directory.Exists("plugins/Paintball/maps")) Directory.CreateDirectory("plugins/Paintball/maps");
            cfg.Save(p.level.name);

            if (p.level == PaintballGame.Instance.Map)
                PaintballGame.Instance.UpdateMapConfig();
        }

        public override void Help(Player p, string message)
        {
            if (message.CaselessEq("h2p"))
            {
                p.Message("&HJoin a Paintball game where it's a free-for-all battle");
                p.Message("&HArmed with paintball weapons, you must eliminate");
                p.Message("&Hall other players while surviving yourself.");
                p.Message("&HLast person standing wins the game.");
            }

            else
            {
                base.Help(p, message);
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/pb start &H- Starts a game of Paintball");
            p.Message("&T/pb stop &H- Immediately stops Paintball");
            p.Message("&T/pb end &H- Ends current round of Paintball");
            p.Message("&T/pb add/remove &H- Adds/removes current map from the map list");
            p.Message("&T/pb clear --confirm &H- Clears all maps from the list");
            p.Message("&T/pb status &H- Outputs current status of Paintball");
            p.Message("&T/pb go &H- Moves you to the current Paintball map.");
        }
    }
}
