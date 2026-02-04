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
        public static Dictionary<string, string> Settings = new Dictionary<string, string>();
        private static Random random = new Random();
        private const string PAINTBALL_MAPS_FILE = "plugins/Paintball/maps.conf";
        private const string PAINTBALL_CONFIG_FILE = "plugins/Paintball/paintball.conf";

        public static string ActiveMap
        {
            get { return Settings.ContainsKey("activemap") ? Settings["activemap"] : ""; }
            set { Settings["activemap"] = value; }
        }

        public static bool IsEnabled
        {
            get { return GetSetting("enabled", "true").CaselessEq("true"); }
            set { Settings["enabled"] = value ? "true" : "false"; }
        }

        public override void Load(bool startup)
        {
            LoadPaintballMaps();
            LoadSettings();
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

        public static void LoadSettings()
        {
            Settings.Clear();
            if (File.Exists(PAINTBALL_CONFIG_FILE))
            {
                string[] lines = File.ReadAllLines(PAINTBALL_CONFIG_FILE);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    int separatorIndex = line.IndexOf('=');
                    if (separatorIndex > 0)
                    {
                        string key = line.Substring(0, separatorIndex).Trim().ToLower();
                        string value = line.Substring(separatorIndex + 1).Trim();
                        Settings[key] = value;
                    }
                }
            }
        }

        public static void SavePaintballMaps()
        {
            Directory.CreateDirectory("plugins/Paintball");
            File.WriteAllLines(PAINTBALL_MAPS_FILE, PaintballMaps.ToArray());
        }

        public static void SaveSettings()
        {
            Directory.CreateDirectory("plugins/Paintball");
            List<string> lines = new List<string>();
            lines.Add("# Paintball Plugin Configuration");
            lines.Add("# Format: key=value");
            lines.Add("");
            
            // Sort keys for consistent output
            List<string> sortedKeys = new List<string>(Settings.Keys);
            sortedKeys.Sort();
            
            foreach (string key in sortedKeys)
            {
                lines.Add(string.Format("{0}={1}", key, Settings[key]));
            }
            
            File.WriteAllLines(PAINTBALL_CONFIG_FILE, lines.ToArray());
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
                
                // Clear active map if it was removed
                if (ActiveMap.CaselessEq(mapName))
                {
                    ActiveMap = "";
                    SaveSettings();
                }
                return true;
            }
            return false;
        }

        public static void ClearPaintballMaps()
        {
            PaintballMaps.Clear();
            ActiveMap = "";
            SavePaintballMaps();
            SaveSettings();
        }

        public static bool SetActiveMap(string mapName)
        {
            // Check if map is in the list
            string existing = PaintballMaps.Find(m => m.CaselessEq(mapName));
            if (existing != null)
            {
                ActiveMap = existing;
                SaveSettings();
                return true;
            }
            return false;
        }

        public static string GetRandomMap()
        {
            if (PaintballMaps.Count == 0) return null;
            int index = random.Next(PaintballMaps.Count);
            return PaintballMaps[index];
        }

        public static bool SetSetting(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;
            
            Settings[key.ToLower()] = value;
            SaveSettings();
            return true;
        }

        public static string GetSetting(string key, string defaultValue = "")
        {
            if (string.IsNullOrWhiteSpace(key))
                return defaultValue;
            
            key = key.ToLower();
            return Settings.ContainsKey(key) ? Settings[key] : defaultValue;
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
            "add", "remove", "rem", "delete", "del", "clear", "set", "enable", "disable"
        };

        public override void Use(Player p, string message, CommandData data)
        {
            string[] args = message.SplitSpaces();
            
            // No arguments - teleport to active paintball map
            if (args.Length < 1)
            {
                TeleportToActivePaintball(p);
                return;
            }

            string action = args[0].ToLower();

            // Check if action requires admin permission
            if (adminActions.Contains(action))
            {
                if (p.Rank < LevelPermission.Admin)
                {
                    p.Message("&cYou must be an Admin to manage Paintball.");
                    return;
                }
            }

            // Handle enable action
            if (action == "enable")
            {
                PaintballPlugin.IsEnabled = true;
                PaintballPlugin.SaveSettings();
                p.Message("&aPaintball teleportation enabled - players can now use /pb to join Paintball maps.");
                return;
            }

            // Handle disable action
            if (action == "disable")
            {
                PaintballPlugin.IsEnabled = false;
                PaintballPlugin.SaveSettings();
                p.Message("&cPaintball teleportation disabled - players cannot use /pb (admins can still manage maps).");
                return;
            }

            // Handle set action
            if (action == "set")
            {
                if (args.Length < 2)
                {
                    p.Message("&cUsage: /pb set <map>");
                    return;
                }

                string mapName = args[1];

                // Check if map is in the paintball maps list
                if (!PaintballPlugin.PaintballMaps.Exists(m => m.CaselessEq(mapName)))
                {
                    p.Message("&cThe map '{0}' is not in the Paintball maps list.", mapName);
                    p.Message("&cUse /pb add <map> to add it first.");
                    return;
                }

                if (PaintballPlugin.SetActiveMap(mapName))
                {
                    p.Message("&aSet '{0}' as the active Paintball map.", PaintballPlugin.ActiveMap);
                }
                return;
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
                ShowUsage(p);
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
                    
                    // If this is the first map, set it as active
                    if (PaintballPlugin.PaintballMaps.Count == 1)
                    {
                        PaintballPlugin.SetActiveMap(mapName);
                        p.Message("&aSet '{0}' as the active Paintball map.", PaintballPlugin.ActiveMap);
                    }
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
                p.Message("&cInvalid action: '{0}'", action);
                ShowUsage(p);
            }
        }

        private void TeleportToActivePaintball(Player p)
        {
            // Check if Paintball is enabled
            if (!PaintballPlugin.IsEnabled)
            {
                p.Message("&cPaintball teleportation is currently disabled.");
                return;
            }

            // Check if there are any paintball maps
            if (PaintballPlugin.PaintballMaps.Count == 0)
            {
                p.Message("&cNo Paintball maps have been configured yet.");
                p.Message("&cAn admin needs to add maps using /pb add <map>");
                return;
            }

            // If no active map is set, pick a random one
            if (string.IsNullOrEmpty(PaintballPlugin.ActiveMap))
            {
                string randomMap = PaintballPlugin.GetRandomMap();
                if (randomMap == null)
                {
                    p.Message("&cFailed to select a Paintball map.");
                    return;
                }
                PaintballPlugin.SetActiveMap(randomMap);
            }

            // Check if active map still exists
            if (string.IsNullOrEmpty(PaintballPlugin.ActiveMap) || !LevelInfo.MapExists(PaintballPlugin.ActiveMap))
            {
                p.Message("&cThe active Paintball map '{0}' no longer exists.", PaintballPlugin.ActiveMap);
                p.Message("&cAn admin needs to set a new active map using /pb set <map>");
                return;
            }

            // Teleport player to the active map
            PlayerActions.ChangeMap(p, PaintballPlugin.ActiveMap);
            p.Message("&aTeleporting you to the Paintball map: &f{0}", PaintballPlugin.ActiveMap);
        }

        private void ShowUsage(Player p)
        {
            p.Message("&cUsage:");
            p.Message("&c  /pb &f- Teleport to the active Paintball map");
            p.Message("&c  /pb add <map> &f- Add a map to the list (Admin)");
            p.Message("&c  /pb remove <map> &f- Remove a map from the list (Admin)");
            p.Message("&c  /pb set <map> &f- Set the active Paintball map (Admin)");
            p.Message("&c  /pb clear --confirm &f- Clear all maps (Admin)");
            p.Message("&c  /pb enable &f- Enable Paintball (Admin)");
            p.Message("&c  /pb disable &f- Disable Paintball (Admin)");
            p.Message("&cUse /help pb for more information");
        }

        public override void Help(Player p)
        {
            p.Message("&T/pb &H- Teleports you to the active Paintball map");
            p.Message("&T/pb add <map> &H- Adds a map to the Paintball maps list (Admin only)");
            p.Message("&T/pb remove/rem/delete/del <map> &H- Removes a map from the list (Admin only)");
            p.Message("&T/pb set <map> &H- Sets the active Paintball map (Admin only)");
            p.Message("&T/pb clear --confirm &H- Clears all maps from the list (Admin only)");
            p.Message("&T/pb enable &H- Enables player teleportation to Paintball (Admin only)");
            p.Message("&T/pb disable &H- Disables player teleportation to Paintball (Admin only)");
        }
    }
}
