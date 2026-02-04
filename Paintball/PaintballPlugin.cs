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
        private const string PAINTBALL_MAPS_FILE = "text/paintballmaps.txt";

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
            Directory.CreateDirectory("text");
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
    }

    public sealed class CmdPaintball : Command2
    {
        public override string name { get { return "Paintball"; } }
        public override string shortcut { get { return "pb"; } }
        public override string type { get { return CommandTypes.Games; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }

        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();
            
            if (args.Length < 2)
            {
                p.Message("Usage: /pb <add|remove> <map>");
                p.Message("Use /help pb for more information");
                return;
            }

            string action = args[0].ToLower();
            string mapName = args[1];

            // Check if map exists
            if (!LevelInfo.MapExists(mapName))
            {
                p.Message("&cMap '{0}' does not exist.", mapName);
                return;
            }

            // Handle add action
            if (action == "add")
            {
                if (PaintballPlugin.AddPaintballMap(mapName))
                {
                    p.Message("&aMap '{0}' has been marked as a Paintball map.", mapName);
                }
                else
                {
                    p.Message("&cMap '{0}' is already a Paintball map.", mapName);
                }
            }
            // Handle remove actions
            else if (action == "remove" || action == "rem" || action == "delete" || action == "del")
            {
                if (PaintballPlugin.RemovePaintballMap(mapName))
                {
                    p.Message("&aMap '{0}' has been removed from Paintball maps.", mapName);
                }
                else
                {
                    p.Message("&cMap '{0}' is not a Paintball map.", mapName);
                }
            }
            else
            {
                p.Message("&cInvalid action. Use: add, remove, rem, delete, or del");
            }
        }

        public override void Help(Player p)
        {
            p.Message("&T/pb add <map> &H- Marks a map as a Paintball map");
            p.Message("&T/pb remove/rem/delete/del <map> &H- Removes a map from Paintball maps");
        }
    }
}
