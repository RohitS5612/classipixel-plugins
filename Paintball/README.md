# Paintball Plugin for MCGalaxy

This plugin adds paintball map management functionality to MCGalaxy servers.

**IMPORTANT:** This plugin only manages a list of Paintball maps. It does NOT delete or modify actual map/level files on your server. Remove and clear operations only affect the Paintball maps list.

## Features

- Teleport to the active Paintball map with a simple command
- Enable/disable player access to Paintball maps
- Add maps to the Paintball maps list (does not modify map files)
- Remove maps from the Paintball maps list (does not delete map files)
- Set which map is currently active for Paintball
- Clear all maps from the list with confirmation (does not delete map files)
- Automatic map existence validation
- Persistent storage in `plugins/Paintball/maps.conf` and `plugins/Paintball/paintball.conf`

## Installation

1. Copy `PaintballPlugin.cs` to your MCGalaxy `plugins` folder
2. Restart your MCGalaxy server or use `/pcompile Paintball` followed by `/pload Paintball`

## Usage

### Teleport to Paintball map:
```
/pb
/paintball
```
Simply typing `/pb` or `/paintball` will teleport you to the currently active Paintball map. If no active map is set, a random map from the list will be selected.

### Add a map to Paintball maps:
```
/pb add <mapname>
```
When you add the first map, it automatically becomes the active map.

### Remove a map from Paintball maps:
```
/pb remove <mapname>
/pb rem <mapname>
/pb delete <mapname>
/pb del <mapname>
```

All remove variations (`remove`, `rem`, `delete`, `del`) perform the same action - they only remove the map from the Paintball list. **The actual map file is NOT deleted from the server.**

### Set the active Paintball map:
```
/pb set <mapname>
```
Sets which map players will be teleported to when using `/pb`. The map must already be in the Paintball maps list.

### Clear all Paintball maps:
```
/pb clear --confirm
```

**IMPORTANT:** This command only clears the Paintball maps list. **The actual map files are NOT deleted from the server.** Running `/pb clear` without `--confirm` will show a confirmation prompt.

## Commands

**Note:** You can use either `/pb` or `/paintball` for all commands below.

- `/pb` or `/paintball` - Teleports you to the active Paintball map (Everyone)
- `/pb add <map>` - Adds a map to the Paintball maps list (Admin only)
- `/pb remove <map>` - Removes a map from the list (Admin only)
- `/pb rem <map>` - Alias for remove (Admin only)
- `/pb delete <map>` - Alias for remove (Admin only)
- `/pb del <map>` - Alias for remove (Admin only)
- `/pb set <map>` - Sets the active Paintball map (Admin only)
- `/pb clear --confirm` - Clears all maps from the list (Admin only)
- `/pb enable` - Enables the Paintball plugin (Admin only)
- `/pb disable` - Disables the Paintball plugin (Admin only)

**Note:** Enable/disable only affects player teleportation with `/pb`. Admins can still manage maps when disabled. Gamemode functionality will be added in future updates.

## Permissions

- Teleport (`/pb`): **Guest** (everyone can teleport to Paintball when enabled)
- Manage maps/settings (add/remove/set/clear/enable/disable): **Admin** (only admins can modify configuration)

## Storage

- Paintball maps list: `plugins/Paintball/maps.conf`
- Settings (including active map): `plugins/Paintball/paintball.conf`

Both files are stored in your MCGalaxy directory (e.g., `/home/rohit/MCGalaxy/plugins/Paintball/`)

The `paintball.conf` file uses a simple `key=value` format and can be edited directly if needed.

## Requirements

- MCGalaxy version 1.9.4.9 or higher
- .NET Framework 4.0 or higher / Mono equivalent

## Notes

- **IMPORTANT:** Remove and clear operations ONLY affect the Paintball maps list - they do NOT delete actual map/level files from your server
- The plugin automatically checks if the specified map exists before adding or removing it
- Map names are case-insensitive (matching MCGalaxy's standard map handling)
- The list of Paintball maps persists across server restarts
- The active map persists across server restarts
- The enabled/disabled state only affects player teleportation to Paintball maps
- When disabled, admins can still manage maps, but players cannot use `/pb` to teleport
- Gamemode functionality (start/stop game on maps) will be added in future updates
- The `clear` command requires confirmation to prevent accidental clearing of the list
- When the first map is added, it automatically becomes the active map
- If the active map is removed, it will be cleared and a new one selected on next `/pb` use
- When Paintball is disabled, players will see a message telling them it's disabled
