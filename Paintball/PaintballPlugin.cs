//reference System.dll
//reference MCGalaxy.exe

using System;
using System.IO;
using System.Collections.Generic;
using MCGalaxy;

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
            if (!PaintballMaps.Contains(mapName))
            {
                PaintballMaps.Add(mapName);
                SavePaintballMaps();
                return true;
            }
            return false;
        }

        public static bool RemovePaintballMap(string mapName)
        {
            if (PaintballMaps.Contains(mapName))
            {
                PaintballMaps.Remove(mapName);
                SavePaintballMaps();
                return true;
            }
            return false;
        }
    }
}
