//reference System.dll
//reference MCGalaxy.exe

using System;
using MCGalaxy;
using MCGalaxy.Commands;

namespace Paintball
{
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
                p.Message("Usage: /pb <add|remove|rem|delete|del> <map>");
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
