# Paintball Plugin for MCGalaxy

This plugin adds paintball map management functionality to MCGalaxy servers.

## Features

- Mark maps as Paintball maps
- Remove maps from the Paintball map list
- Automatic map existence validation
- Persistent storage of Paintball map list

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

## Commands

- `/pb add <map>` - Marks a map as a Paintball map
- `/pb remove <map>` - Removes a map from Paintball maps
- `/pb rem <map>` - Alias for remove
- `/pb delete <map>` - Alias for remove
- `/pb del <map>` - Alias for remove

## Permissions

- Command access: **Guest** (anyone can use the command)
- Add/Remove maps: **Admin** (only admins can modify the map list)

Future commands will be available to all players, but map management requires admin privileges.

## Storage

Paintball maps are stored in `text/paintballmaps.txt` in your MCGalaxy directory.

## Requirements

- MCGalaxy version 1.9.4.9 or higher
- .NET Framework 4.0 or higher / Mono equivalent

## Notes

- The plugin automatically checks if the specified map exists before adding or removing it
- Map names are case-insensitive (matching MCGalaxy's standard map handling)
- The list of Paintball maps persists across server restarts
