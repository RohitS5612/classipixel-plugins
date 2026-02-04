# Paintball Plugin for MCGalaxy

This plugin adds paintball map management functionality to MCGalaxy servers.

## Features

- Add maps to the Paintball maps list
- Remove maps from the Paintball maps list
- Clear all maps from the list with confirmation
- Automatic map existence validation
- Persistent storage in `plugins/Paintball/maps.conf`

## Installation

1. Copy `PaintballPlugin.cs` to your MCGalaxy `plugins` folder
2. Restart your MCGalaxy server or use `/pcompile Paintball` followed by `/pload Paintball`

## Usage

### Add a map to Paintball maps:
```
/pb add <mapname>
```

### Remove a map from Paintball maps:
```
/pb remove <mapname>
/pb rem <mapname>
/pb delete <mapname>
/pb del <mapname>
```

All remove variations (`remove`, `rem`, `delete`, `del`) perform the same action.

### Clear all Paintball maps:
```
/pb clear --confirm
```

**Note:** Running `/pb clear` without `--confirm` will show a confirmation prompt to prevent accidental deletion.

## Commands

- `/pb add <map>` - Adds a map to the Paintball maps list (Admin only)
- `/pb remove <map>` - Removes a map from the list (Admin only)
- `/pb rem <map>` - Alias for remove (Admin only)
- `/pb delete <map>` - Alias for remove (Admin only)
- `/pb del <map>` - Alias for remove (Admin only)
- `/pb clear --confirm` - Clears all maps from the list (Admin only)

## Permissions

- Command access: **Guest** (anyone can use the command)
- Add/Remove/Clear maps: **Admin** (only admins can modify the map list)

Future commands will be available to all players, but map management requires admin privileges.

## Storage

Paintball maps are stored in `plugins/Paintball/maps.conf` in your MCGalaxy directory.

## Requirements

- MCGalaxy version 1.9.4.9 or higher
- .NET Framework 4.0 or higher / Mono equivalent

## Notes

- The plugin automatically checks if the specified map exists before adding or removing it
- Map names are case-insensitive (matching MCGalaxy's standard map handling)
- The list of Paintball maps persists across server restarts
- The `clear` command requires confirmation to prevent accidental data loss
