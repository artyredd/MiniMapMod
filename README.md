# Mini Map Mod
From the creator of [SkipWelcomeScreen](https://thunderstore.io/package/DekuDesu/SkipWelcomeScreen/) brings you the Mini Map Mod. Everything you ever wanted. If what you ever wanted was a simple MiniMap that is, but let's be real, who wouldn't have that as their dream?
### Features

- Implements a Mini Map In-Game.
- Single and Multiplayer Compatible.
- Plays Nice with 95% of Most Downloaded Mods.

### Showcase

![](https://i.imgur.com/M24wdg4.jpg)
	
![](https://i.imgur.com/A1MnIwJ.jpg)
	
![](https://i.imgur.com/tFgHT92.jpg)
	
### Dependencies
- BepInEx

### Installation
- Place MiniMapMod.dll into your BepInEx/plugins folder.

### How to Use
- Use your arrows keys(Default) to move the MiniMap to desired location.
- Use your NumPadMinus(Default) and NumPadPlus(Default) to zoom the Mini Map in and out.
- Use PageUp(Default) and PageDown(Default) to increase and decrease the size of the Mini Map.
- The position, size, and zoom of the Mini Map is saved automatically.
- Use M(Default) to lock the Mini Map in place if you don't want to accidentally move the MiniMap during play.
- You can change the (Default) keys above by using the config in *cofigs/MiniMapMod.conf*

### Config
```
[Controls]

## Key, when pressed, toggles locking the minimaps position and size in place until pressed again.
# Setting type: KeyCode
# Default value: M
# A,B,C,D,E,F... (See more in actual Config file)
Enable_Moving_Minimap_Key = M

## Move MiniMap: Up
# Setting type: KeyCode
# Default value: UpArrow
# A,B,C,D,E,F... (See more in actual Config file)
Move_Up = UpArrow

## Move MiniMap: Down
# Setting type: KeyCode
# Default value: DownArrow
# A,B,C,D,E,F... (See more in actual Config file)
Move_Down = DownArrow

## Move MiniMap: Left
# Setting type: KeyCode
# Default value: LeftArrow
# A,B,C,D,E,F... (See more in actual Config file)
Move_Left = LeftArrow

## Move MiniMap: Right
# Setting type: KeyCode
# Default value: RightArrow
# A,B,C,D,E,F... (See more in actual Config file)
Move_Right = RightArrow

## Increases MiniMaps Size
# Setting type: KeyCode
# Default value: PageUp
# A,B,C,D,E,F... (See more in actual Config file)
Increase_Size = PageUp

## Decreases MiniMaps Size
# Setting type: KeyCode
# Default value: PageDown
# A,B,C,D,E,F... (See more in actual Config file)
Decrease_Size = PageDown

## Zooms minimap In
# Setting type: KeyCode
# Default value: Plus
# A,B,C,D,E,F... (See more in actual Config file)
Zoom_In = KeypadPlus

## Zooms MiniMap Out
# Setting type: KeyCode
# Default value: Minus
# A,B,C,D,E,F... (See more in actual Config file)
Zoom_Out = KeypadMinus

[Enable Mod]

## Enables/Disables MiniMapMod
# Setting type: Boolean
# Default value: True
EnableMod = true

[Position and Size]

## Starting Position of MiniMap
# Setting type: Single
# Default value: 0
Horizontal_Starting_Position = 1545

## Starting Position of MiniMap
# Setting type: Single
# Default value: 0
Vertical_Starting_Position = -297

## Width of MiniMap
# Setting type: Single
# Default value: 100
Minimap_Width = 258

## Width of MiniMap
# Setting type: Single
# Default value: 100
Minimap_Height = 258

## How far the minimap is zoomed in
# Setting type: Single
# Default value: 85
Minimap_Zoom_Level = 85


```

### Upcoming Changes
- Toggle-able round/square MiniMap styles.
- Toggle-able icons that help delineate interactables on the Mini Map from eachother.
- Change-able colors for Mini Map background color, transparency, and marker colors.
### If you like my Mod please consider donating to the Autism Research Institute 
https://www.autism.org/donate-autism-research-institute/

### Bug Reports
If you have an issue, discover a bug, or have a recommendation please drop me a line directly through GitHub!

### Change Log
First Release 1.0.0
- Implemented MiniMap
