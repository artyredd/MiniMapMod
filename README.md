# Mini Map Mod
Adds a contemporary Minimap to the game.

### Features

- Implements a Mini Map In-Game.
- Show interactable items, shrines, teleporters, enemies and more.
- Single and Multiplayer Compatible.
- Every icon is configurable for color, size and can be toggled on/off.

### Showcase

![](https://i.imgur.com/f4VwvNF.jpg)
	
### Dependencies
- BepInEx

### Manual Installation
- Place MiniMapLibrary.dll into your BepInEx/plugins folder.
- Place MiniMapMod.dll into your BepInEx/plugins folder.

### How to Use
- Use 'M' to hide or show the minimap. 

### Troubleshooting
- Toggle 'M' to hide and show minimap to force a refresh and/or a re-draw if something wonky is happening on the minimap.

### Config
BepInEx Config. Each icon is individually configurable.
- Enabled / Disabled
- Active Color
- Inactive Color
- Icon Width
- Icon Height

### Upcoming Changes
None - Final Release

### If you like my Mod please consider donating to the Autism Research Institute 
https://www.autism.org/donate-autism-research-institute/

### If you REALLY like my code I am looking for work
Contact me for my resume

### Bug Reports
If you have an issue, discover a bug, or have a recommendation please file an issue on my github page.

### Change Log
Hotfix 3.1.6
- fixed typo causing all objects on map being selected to be placed on minimap regardless if they were filtered out

Hotfix 3.1.5
- prevented mod from throwing exceptions within global events causing other mods to not have their events fired
- added compatibility for mods that implement ChestBehaviour but not PurchaseInteraction
- added debug options and logging

Minor 3.1.4
- removed stealth chests from being shown on the minimap
- removed transport fans from chest icon to Icon.Special icons

Minor 3.1.3
- added ability to change minimap icon paths ( limited to streaming assets )
- refactored sprite cache

Minor 3.1.2
- refactor of config code (github only update)

Minor 3.1.1
- fixed adapative chests not appearing on minimap(hopefully)
- added descriptions to the config sections
- removed Icon.None and Icon.Primary sections from config
- added seperate lunar pod icons and config section

Major 3.1
- Added config toggle to enable or disable each individual icon
- Added config option to choose color for each individual icon
- Added config option to choose icon height and width for each individual icon

Minor 3.0.1
- un-spammed console

Compatibility Patch 3.0.0  
- Added compatability for Survivors of the Void
- Updated to new Unity Reference packages
- Updated to new BepInEx/RoR2 API

Hotfix 2.04
- enabled mini-map by default, making it so players dont need to remember to press 'M' to show it every time they start the game
- performance minor

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
