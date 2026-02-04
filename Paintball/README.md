# Paintball Plugin for MCGalaxy

This plugin adds paintball map management functionality to MCGalaxy servers.

## Features

- Teleport to the active Paintball map with a simple command
- Add maps to the Paintball maps list
- Remove maps from the Paintball maps list
- Set which map is currently active for Paintball
- Clear all maps from the list with confirmation
- Automatic map existence validation
- Persistent storage in `plugins/Paintball/maps.conf` and `plugins/Paintball/active.txt`

## Installation

1. Copy `PaintballPlugin.cs` to your MCGalaxy `plugins` folder
2. Restart your MCGalaxy server or use `/pcompile Paintball` followed by `/pload Paintball`

## Usage

### Teleport to Paintball map:
```
/pb
```
Simply typing `/pb` will teleport you to the currently active Paintball map. If no active map is set, a random map from the list will be selected.

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

All remove variations (`remove`, `rem`, `delete`, `del`) perform the same action.

### Set the active Paintball map:
```
/pb set <mapname>
```
Sets which map players will be teleported to when using `/pb`. The map must already be in the Paintball maps list.

### Clear all Paintball maps:
```
/pb clear --confirm
```

**Note:** Running `/pb clear` without `--confirm` will show a confirmation prompt to prevent accidental deletion.

## Commands

- `/pb` - Teleports you to the active Paintball map (Everyone)
- `/pb add <map>` - Adds a map to the Paintball maps list (Admin only)
- `/pb remove <map>` - Removes a map from the list (Admin only)
- `/pb rem <map>` - Alias for remove (Admin only)
- `/pb delete <map>` - Alias for remove (Admin only)
- `/pb del <map>` - Alias for remove (Admin only)
- `/pb set <map>` - Sets the active Paintball map (Admin only)
- `/pb clear --confirm` - Clears all maps from the list (Admin only)

## Permissions

- Teleport (`/pb`): **Guest** (everyone can teleport to Paintball)
- Manage maps (add/remove/set/clear): **Admin** (only admins can modify the map list)

## Storage

- Paintball maps list: `plugins/Paintball/maps.conf`
- Settings (including active map): `plugins/Paintball/paintball.conf`

Both files are stored in your MCGalaxy directory (e.g., `/home/rohit/MCGalaxy/plugins/Paintball/`)

The `paintball.conf` file uses a simple `key=value` format and can be edited directly if needed.

## Requirements

- MCGalaxy version 1.9.4.9 or higher
- .NET Framework 4.0 or higher / Mono equivalent

## Notes

- The plugin automatically checks if the specified map exists before adding or removing it
- Map names are case-insensitive (matching MCGalaxy's standard map handling)
- The list of Paintball maps persists across server restarts
- The active map persists across server restarts
- The `clear` command requires confirmation to prevent accidental data loss
- When the first map is added, it automatically becomes the active map
- If the active map is removed, it will be cleared and a new one selected on next `/pb` use
