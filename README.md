# Mini Map Mod
Adds a contemporary Minimap to the game.

### Features

- Implements a Mini Map In-Game.
- Show interactable items, shrines, teleporters, enemies and more.
- Single and Multiplayer Compatible.
- Plays Nice with 95% of Most Downloaded Mods.

### Showcase

![](https://i.imgur.com/f4VwvNF.jpg)
	
### Dependencies
- BepInEx

### Installation
- Place MiniMapLibrary.dll into your BepInEx/plugins folder.
- Place MiniMapMod.dll into your BepInEx/plugins folder.

### How to Use
- Use 'M' to hide or show the minimap. 

### Troubleshooting
- Toggle 'M' to hide and show minimap to force a refresh and/or a re-draw if something wonky is happening on the minimap.

### Config
No config applicable at this time

### Upcoming Changes
None - Final Release

### If you like my Mod please consider donating to the Autism Research Institute 
https://www.autism.org/donate-autism-research-institute/

### Bug Reports
If you have an issue, discover a bug, or have a recommendation please file an issue on my github page.

### Change Log
Hotfixes 2.03
- changed icon sizes
- added support for colors
- added compatibility for mods that destroy interactibles(that would have crashed the minimap)
- performance fix for calculating minimap position
- performance fix for scene scanning
- added enemies to the minimap
- added support for changing inactive or bought items/interactibles to a different color
- changed teleporter icon size and color
- added color(green) for teleporter icon when it is done charging
- performance fix for runs in HAHA.. difficulty and enemy count becomes excessive
- performance fix for runs with items that auto-kill enemies instantly(removes flashing minimap)
- removed excessive logging from the console

Hotfix 2.0.2
- Fixed critical bug duplicating icons on the minimap when using interactables
- Typo in change log

Final Release 2.0.0
- Completely Re-Implemented minimap  

First Release 1.0.0
- Implemented MiniMap
