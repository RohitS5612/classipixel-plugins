//reference System.dll
//reference MCGalaxy.exe

using System;
using System.IO;
using System.Collections.Generic;
using MCGalaxy;
using MCGalaxy.Commands;

namespace Paintball
{
    public sealed class PaintballPlugin : Plugin
    {
        public override string name { get { return "Paintball"; } }
        public override string MCGalaxy_Version { get { return "1.9.4.9"; } }
        public override string creator { get { return "Classipixel"; } }

        public static List<string> PaintballMaps = new List<string>();
        private const string PAINTBALL_MAPS_FILE = "plugins/Paintball/maps.conf";

        public override void Load(bool startup)
        {
            LoadPaintballMaps();
            Command.Register(new CmdPaintball());
        }

        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("Paintball"));
        }

        public static void LoadPaintballMaps()
        {
            PaintballMaps.Clear();
            if (File.Exists(PAINTBALL_MAPS_FILE))
            {
                string[] lines = File.ReadAllLines(PAINTBALL_MAPS_FILE);
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        PaintballMaps.Add(line.Trim());
                    }
                }
            }
        }

        public static void SavePaintballMaps()
        {
            Directory.CreateDirectory("plugins/Paintball");
            File.WriteAllLines(PAINTBALL_MAPS_FILE, PaintballMaps.ToArray());
        }

        public static bool AddPaintballMap(string mapName)
        {
            // Use case-insensitive comparison to match MCGalaxy's map handling
            if (!PaintballMaps.Exists(m => m.CaselessEq(mapName)))
            {
                PaintballMaps.Add(mapName);
                SavePaintballMaps();
                return true;
            }
            return false;
        }

        public static bool RemovePaintballMap(string mapName)
        {
            // Use case-insensitive comparison to match MCGalaxy's map handling
            string existing = PaintballMaps.Find(m => m.CaselessEq(mapName));
            if (existing != null)
            {
                PaintballMaps.Remove(existing);
                SavePaintballMaps();
                return true;
            }
            return false;
        }

        public static void ClearPaintballMaps()
        {
            PaintballMaps.Clear();
            SavePaintballMaps();
        }
    }

    public sealed class CmdPaintball : Command2
    {
        public override string name { get { return "Paintball"; } }
        public override string shortcut { get { return "pb"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Guest; } }

        // Actions that require admin permission
        private static readonly HashSet<string> adminActions = new HashSet<string> 
        {
            "add", "remove", "rem", "delete", "del", "clear"
        };

        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();
            
            if (args.Length < 1)
            {
                p.Message("&cUsage: /pb <add|remove|clear> <map|--confirm>");
                p.Message("&cUse /help pb for more information");
                return;
            }

            string action = args[0].ToLower();

            // Check if action requires admin permission
            if (adminActions.Contains(action))
            {
                if (p.Rank < LevelPermission.Admin)
                {
                    p.Message("&cYou must be an Admin to manage Paintball maps.");
                    return;
                }
            }

            // Handle clear action
            if (action == "clear")
            {
                if (args.Length < 2 || args[1] != "--confirm")
                {
                    p.Message("&cAre you sure you want to clear all Paintball maps?");
                    p.Message("&cUse: &f/pb clear --confirm &cto proceed.");
                    return;
                }

                int count = PaintballPlugin.PaintballMaps.Count;
                PaintballPlugin.ClearPaintballMaps();
                p.Message("&aCleared all {0} Paintball map(s) from the list.", count);
                return;
            }

            // For add/remove actions, we need a map name
            if (args.Length < 2)
            {
                p.Message("&cUsage: /pb <add|remove> <map>");
                p.Message("&cUse /help pb for more information");
                return;
            }

            string mapName = args[1];

            // Check if map exists
            if (!LevelInfo.MapExists(mapName))
            {
                p.Message("&cThe map '{0}' does not exist on this server.", mapName);
                return;
            }

            // Handle add action
            if (action == "add")
            {
                if (PaintballPlugin.AddPaintballMap(mapName))
                {
                    p.Message("&aSuccessfully added '{0}' to the Paintball maps list.", mapName);
                }
                else
                {
                    p.Message("&cThe map '{0}' is already in the Paintball maps list.", mapName);
                }
            }
            // Handle remove actions
            else if (action == "remove" || action == "rem" || action == "delete" || action == "del")
            {
                if (PaintballPlugin.RemovePaintballMap(mapName))
                {
                    p.Message("&aSuccessfully removed '{0}' from the Paintball maps list.", mapName);
                }
                else
                {
                    p.Message("&cThe map '{0}' is not in the Paintball maps list.", mapName);
                }
            }
            else
            {
                p.Message("&cInvalid action. Use: add, remove, rem, delete, del, or clear");
                p.Message("&cUse /help pb for more information");
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/pb add <map> &H- Adds a map to the Paintball maps list (Admin only)");
            p.Message("&T/pb remove/rem/delete/del <map> &H- Removes a map from the list (Admin only)");
            p.Message("&T/pb clear --confirm &H- Clears all maps from the list (Admin only)");
        }
    }
}
