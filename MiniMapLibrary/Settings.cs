using System;
using System.Collections.Generic;
using MiniMapLibrary.Config;
using UnityEngine;

namespace MiniMapLibrary
{
    // Names are part of MiniMap.cfg; don't rename.
    public enum LogLevel { none, info, debug, all }

    public static class Settings
    {
        public static class Icons
        {
            public const string Default = "Textures/MiscIcons/texMysteryIcon";
            public const string LootBag = "Textures/MiscIcons/texLootIconOutlined";
            public const string Chest = "Textures/MiscIcons/texInventoryIconOutlined";
            public const string Circle = "Textures/MiscIcons/texBarrelIcon";
            public const string Shrine = "Textures/MiscIcons/texShrineIconOutlined";
            public const string Boss = "Textures/MiscIcons/texTeleporterIconOutlined";
            public const string Drone = "Textures/MiscIcons/texDroneIconOutlined";
            public const string Cross = "Textures/MiscIcons/texCriticallyHurtIcon";
            public const string Lock = "Textures/MiscIcons/texUnlockIcon";
            public const string Dice = "Textures/MiscIcons/texRuleMapIsRandom";
            public const string Cube = "Textures/MiscIcons/texLunarPillarIcon";
            public const string Wrench = "Textures/MiscIcons/texWIPIcon";
            public const string Attack = "Textures/MiscIcons/texAttackIcon";
            public const string Sprint = "Textures/MiscIcons/texSprintIcon";
            public const string Arrow = "Textures/MiscIcons/texOptionsArrowLeft";
            public const string Pencil = "Textures/MiscIcons/texEditIcon";
            public const string Portal = "Textures/MiscIcons/texQuickplay";
            public const string Lunar = "Textures/ItemIcons/BG/texLunarBGIcon";
            // Not a game asset: SpriteManager draws this one itself.
            public const string Dot = "MiniMap/Dot";
        }

        public static class Colors
        {
            public static Color Yellow = new(1f, 219f / 255f, 88f / 255f);
            public static Color Purple = new(122f / 255f, 105f / 255f, 244f / 255f);
            public static Color DarkPurple = new(76f / 255f, 53f / 255f, 244f / 255f);
            public static Color Teal = new(94f / 255f, 178f / 255f, 242f / 255f);
            public static Color Pink = new(172f / 255f, 95f / 255f, 243f / 255f);
            public static Color DarkPink = new(146f / 255f, 39f / 255f, 243f / 255f);
            public static Color Orange = new(1f, 146f / 255f, 0f);
            public static Color DarkTeal = new(94f / 255f, 178f / 255f, 242f / 255f);
            public static Color LightCyan = new(156f / 255f, 245f / 255f, 1f);
        }

        public static Dimension2D MinimapSize { get; set; } = new(180f, 180f);
        public static Dimension2D ViewfinderSize { get; set; } = new(180f, 180f);
        public static IDictionary<InteractableKind, InteractibleSetting> InteractibleSettings { get; } = new Dictionary<InteractableKind, InteractibleSetting>();
        public static Color PlayerIconColor { get; set; } = Color.white;
        public static Color DefaultActiveColor { get; set; } = Colors.Yellow;
        public static Color DefaultInactiveColor { get; set; } = Color.grey;
        public static Color DefaultElevationMarkerColor { get; set; } = Color.white;
        public static Dimension2D DefaultElevationSize { get; set; } = new(3f, 3f);

        private static readonly InteractibleSetting fallbackSetting = new()
        {
            Dimensions = new Dimension2D(11f, 11f),
            ActiveColor = Colors.Yellow,
            InactiveColor = Color.grey,
            IconPath = Icons.Default,
            Description = "Unknown interactable",
            ElevationMarkerDimensions = new Dimension2D(3f, 3f),
            ElevationMarkerOffset = new Dimension2D(0f, 0f)
        };

        private const int CurrentConfigRevision = 1;
        private static ILogger logger;
        private static IConfigEntry<int> configRevision;
        private static IConfigEntry<LogLevel> logLevel;
        private static IConfigEntry<KeyCode> minimapKey, increaseScaleKey, decreaseScaleKey;
        private static IConfigEntry<float> minimapScale, interactionScanInterval, bodyScanInterval, stateRefreshInterval, sceneScanInterval;
        private static IConfigEntry<float> mapWidth, mapHeight, worldRadius, elevationThreshold;
        private static IConfigEntry<bool> rotateWithCamera, showUnclassifiedInteractables;
        private static IConfigEntry<Color> backgroundColor;
        private static IConfigEntry<Vector2> positionOffset;

        public static LogLevel LogLevel => logLevel?.Value ?? LogLevel.info;
        public static KeyCode MinimapKey => minimapKey?.Value ?? KeyCode.M;
        public static KeyCode MinimapIncreaseScaleKey => increaseScaleKey?.Value ?? KeyCode.Equals;
        public static KeyCode MinimapDecreaseScaleKey => decreaseScaleKey?.Value ?? KeyCode.Minus;
        public static float MinimapScale => Numeric.Clamp(minimapScale?.Value ?? 1f, 0.1f, 20f, 1f);
        public static float InteractionScanInterval => Numeric.Clamp(interactionScanInterval?.Value ?? 1f, 0.25f, 30f, 1f);
        public static float BodyScanInterval => Numeric.Clamp(bodyScanInterval?.Value ?? 0.25f, 0.1f, 5f, 0.25f);
        public static float StateRefreshInterval => Numeric.Clamp(stateRefreshInterval?.Value ?? 0.2f, 0.1f, 5f, 0.2f);
        public static float SceneScanInterval => Numeric.Clamp(sceneScanInterval?.Value ?? 5f, 2f, 60f, 5f);
        public static bool DebugLogging => LogLevel >= LogLevel.debug;
        public static float WorldRadius => Numeric.Clamp(worldRadius?.Value ?? 0f, 0f, 100000f, 0f);
        public static float ElevationThreshold => Numeric.Clamp(elevationThreshold?.Value ?? 5f, 0f, 10000f, 5f);
        public static bool RotateWithCamera => rotateWithCamera?.Value ?? true;
        public static bool ShowUnclassifiedInteractables => showUnclassifiedInteractables?.Value ?? true;
        public static Color BackgroundColor => SanitizeColor(backgroundColor?.Value ?? new Color(0.03f, 0.04f, 0.06f, 0.65f));
        public static Vector2 PositionOffset
        {
            get
            {
                Vector2 offset = positionOffset?.Value ?? new Vector2(0f, -8f);
                return new Vector2(Numeric.Clamp(offset.x, -4096f, 4096f, 0f), Numeric.Clamp(offset.y, -4096f, 4096f, -8f));
            }
        }

        static Settings()
        {
            Add(InteractableKind.Chest, 9f, 7f, "Chests and adaptive chests", Icons.Chest);
            Add(InteractableKind.StealthChest, 9f, 7f, "Cloaked chests (reveals their locations when enabled)", Icons.Chest, enabled: false);
            Add(InteractableKind.TimedChest, 9f, 7f, "Timed security chests (grey after locking or opening)", Icons.Chest, enabled: false);
            Add(InteractableKind.Shop, 8f, 6f, "Shops and terminals", Icons.Chest, Colors.Teal);
            Add(InteractableKind.Equipment, 9f, 7f, "Equipment barrels", Icons.Chest, Colors.Orange);
            Add(InteractableKind.Printer, 11f, 9f, "Printers", Icons.Chest, Colors.Purple);
            Add(InteractableKind.Utility, 6f, 6f, "Scrappers, cleansing pools and drone stations", Icons.Wrench);
            Add(InteractableKind.LunarPod, 8f, 8f, "Lunar pods", Icons.LootBag, Color.cyan);
            Add(InteractableKind.Shrine, 11f, 11f, "Shrines (excluding Newt and the Altar of Gold)", Icons.Shrine);
            Add(InteractableKind.NewtAltar, 11f, 11f, "Newt altars", Icons.Lunar);
            Add(InteractableKind.Teleporter, 17f, 17f, "Teleporters", Icons.Boss, Color.white, Color.green);
            Add(InteractableKind.Barrel, 3f, 3f, "Barrels", Icons.Circle);
            Add(InteractableKind.Drone, 8f, 8f, "Broken drones and turrets", Icons.Drone);
            Add(InteractableKind.Special, 6f, 6f, "Other interactables, landing pods, fans and stage objectives", Icons.Default);
            Add(InteractableKind.EnemyMonster, 3f, 3f, "Enemies", Icons.Circle, Color.red, elevationColor: Color.red);
            Add(InteractableKind.EnemyLunar, 3f, 3f, "Lunar enemies", Icons.Circle, Color.red, elevationColor: Color.red);
            Add(InteractableKind.EnemyVoid, 13f, 13f, "Void enemies", Icons.Attack, Color.magenta, elevationColor: Color.magenta);
            Add(InteractableKind.EnemyEndOfCycle, 4f, 4f, "End of the Cycle enemies (Hallowed Concepts)", Icons.Attack, Colors.Orange, elevationColor: Colors.Orange);
            Add(InteractableKind.Minion, 3f, 3f, "Allied drones and minions", Icons.Circle, Color.green, elevationColor: Color.green);
            Add(InteractableKind.Player, 9f, 9f, "The viewed character and other players", Icons.Sprint, PlayerIconColor, PlayerIconColor);
            Add(InteractableKind.Item, 3f, 3f, "Item choice terminals that are not dropped pickups", Icons.Circle, Colors.Teal, Color.cyan);
            Add(InteractableKind.Portal, 8f, 8f, "Portals, the Altar of Gold and ritual gongs", Icons.Portal);
            Add(InteractableKind.Totem, 8f, 8f, "Halcyon beacons and power pedestals", Icons.Cube, Colors.DarkTeal);
            Add(InteractableKind.Neutral, 4f, 4f, "Neutral objects and NPCs", Icons.Circle, Color.white);
            Add(InteractableKind.Pickup, 2f, 2f, "Dropped items, equipment, lunar coins, command cubes and void potentials", Icons.Dot,
                Colors.LightCyan, elevationMarker: false);
        }

        // 4.1.x (and upstream 4.0.0) defaults. Configs still holding these values
        // are moved to the current defaults once; customised sizes are left alone.
        private static readonly Dictionary<InteractableKind, (float Width, float Height)> legacySizes = new()
        {
            [InteractableKind.Chest] = (8f, 6f),
            [InteractableKind.StealthChest] = (8f, 6f),
            [InteractableKind.TimedChest] = (8f, 6f),
            [InteractableKind.Shop] = (7f, 5f),
            [InteractableKind.Equipment] = (8f, 6f),
            [InteractableKind.Printer] = (10f, 8f),
            [InteractableKind.Utility] = (5f, 5f),
            [InteractableKind.LunarPod] = (7f, 7f),
            [InteractableKind.Shrine] = (10f, 10f),
            [InteractableKind.NewtAltar] = (10f, 10f),
            [InteractableKind.Teleporter] = (15f, 15f),
            [InteractableKind.Drone] = (7f, 7f),
            [InteractableKind.Special] = (5f, 5f),
            [InteractableKind.EnemyVoid] = (12f, 12f),
            [InteractableKind.Player] = (8f, 8f),
            [InteractableKind.Portal] = (7f, 7f),
            [InteractableKind.Totem] = (7f, 7f),
        };

        private static readonly Dictionary<InteractableKind, (float Width, float Height)> defaultSizes = new();

        private static void Add(InteractableKind kind, float width, float height, string description, string path,
            Color? activeColor = null, Color? inactiveColor = null, Color? elevationColor = null, bool enabled = true, bool elevationMarker = true)
        {
            defaultSizes.Add(kind, (width, height));
            InteractibleSettings.Add(kind, new InteractibleSetting
            {
                Enabled = enabled,
                Dimensions = new Dimension2D(width, height),
                ActiveColor = activeColor ?? DefaultActiveColor,
                InactiveColor = inactiveColor ?? DefaultInactiveColor,
                Description = description,
                IconPath = path,
                ElevationMarkerEnabled = elevationMarker,
                ElevationMarkerColor = elevationColor ?? DefaultElevationMarkerColor,
                ElevationMarkerDimensions = new Dimension2D(DefaultElevationSize.Width, DefaultElevationSize.Height),
                ElevationMarkerOffset = ElevationOffset(width, height)
            });
        }

        private static Dimension2D ElevationOffset(float width, float height) =>
            new((width + DefaultElevationSize.Width) * 0.5f, (height + DefaultElevationSize.Height) * 0.5f);

        public static InteractibleSetting GetSetting(InteractableKind type) =>
            InteractibleSettings.TryGetValue(type, out InteractibleSetting setting) ? setting : fallbackSetting;

        public static Color GetColor(InteractableKind type, bool active)
        {
            InteractibleSetting setting = GetSetting(type);
            return active ? setting.ActiveColor : setting.InactiveColor;
        }

        public static Dimension2D GetInteractableSize(InteractableKind type) => GetSetting(type).Dimensions;

        public static void LoadApplicationSettings(ILogger logger, IConfig config)
        {
            Settings.logger = logger;
            logLevel = config.Bind("Settings.General", "LogLevel", LogLevel.info, "Console logging detail. Errors and warnings are always logged.");
            minimapKey = config.Bind("Settings.General", "EnableKey", KeyCode.M, "Show or hide the minimap.");
            increaseScaleKey = config.Bind("Settings.Zoom", "ZoomKey", KeyCode.Equals, "Zoom in.");
            decreaseScaleKey = config.Bind("Settings.Zoom", "UnZoomKey", KeyCode.Minus, "Zoom out.");
            minimapScale = config.Bind("Settings.Zoom", "zoomLevel", 1f, "Saved map zoom (0.1-20). Icon sizes remain readable while zooming.");
            interactionScanInterval = config.Bind("Settings.Performance", "InteractableScanInterval", 1f, "Seconds between interactable registry and cached scene-candidate refreshes (0.25-30).");
            bodyScanInterval = config.Bind("Settings.Performance", "BodyScanInterval", 0.25f, "Seconds between character and dropped pickup refreshes (0.1-5).");
            stateRefreshInterval = config.Bind("Settings.Performance", "StateRefreshInterval", 0.2f, "Seconds between icon availability/color checks (0.1-5). Positions update each frame.");
            sceneScanInterval = config.Bind("Settings.Performance", "SceneScanInterval", 5f, "Seconds between fallback searches for objects missing from the game's registries (2-60). After the first few passes of a stage it only keeps running if such objects were found.");
            mapWidth = config.Bind("Settings.Layout", "Width", 180f, "Map width in HUD units (60-800), affected by the game's HUD scale.");
            mapHeight = config.Bind("Settings.Layout", "Height", 180f, "Map height in HUD units (60-800).");
            positionOffset = config.Bind("Settings.Layout", "PositionOffset", new Vector2(0f, -8f), "Offset from the map's reserved HUD space. Positive X moves right; positive Y moves up.");
            backgroundColor = config.Bind("Settings.Layout", "BackgroundColor", new Color(0.03f, 0.04f, 0.06f, 0.65f), "Background color and opacity. Alpha zero makes it transparent.");
            rotateWithCamera = config.Bind("Settings.Layout", "RotateWithCamera", true, "Rotate with the camera. Disable for a north-up map.");
            worldRadius = config.Bind("Settings.Zoom", "WorldRadius", 0f, "World units from center to the shortest edge at zoom 1. Zero fits stage bounds automatically.");
            elevationThreshold = config.Bind("Settings.General", "ElevationThreshold", 5f, "Vertical distance before an elevation arrow appears.");
            configRevision = config.Bind("Settings.General", "ConfigRevision", 0, "Used to migrate old default values. Do not change.");
            showUnclassifiedInteractables = config.Bind("Settings.General", "ShowUnclassifiedInteractables", true, "Show other scanner-visible interactables as Special, including new DLC and modded objects registered with the game.");
            Refresh();
        }

        public static void LoadConfigEntries(IConfig config)
        {
            foreach (InteractableKind kind in InteractibleSettings.Keys)
                LoadConfigEntries(kind, config);
            if (configRevision is IMutableConfigEntry<int> revision && revision.Value < CurrentConfigRevision)
                revision.Value = CurrentConfigRevision;
        }

        public static void LoadConfigEntries(InteractableKind type, IConfig config)
        {
            InteractableKind kind = type;
            if (!InteractibleSettings.TryGetValue(kind, out InteractibleSetting setting))
            {
                logger?.LogError($"No icon settings registered for {kind}.");
                return;
            }

            string section = $"Icon.{kind}";
            setting.Config = new SettingConfigGroup(
                config.Bind(section, "enabled", setting.Enabled, $"Show {setting.Description} on the minimap."),
                config.Bind(section, "height", setting.Dimensions.Height, "Icon height in HUD units."),
                config.Bind(section, "width", setting.Dimensions.Width, "Icon width in HUD units."),
                config.Bind(section, "activeColor", setting.ActiveColor, "Color while available."),
                config.Bind(section, "inactiveColor", setting.InactiveColor, "Color after use or while unavailable."),
                config.Bind(section, "icon", setting.IconPath, "Legacy resource path of the icon. Existing icon paths remain supported."),
                config.Bind(section, "elevationMarkerEnabled", setting.ElevationMarkerEnabled, "Show an elevation marker."),
                config.Bind(section, "elevationMarkerOffset", new Vector2(setting.ElevationMarkerOffset.Width, setting.ElevationMarkerOffset.Height), "Elevation marker offset from the icon center."),
                config.Bind(section, "elevationMarkerHeight", setting.ElevationMarkerDimensions.Height, "Elevation marker height."),
                config.Bind(section, "elevationMarkerWidth", setting.ElevationMarkerDimensions.Width, "Elevation marker width."),
                config.Bind(section, "elevationMarkerColor", setting.ElevationMarkerColor, "Elevation marker color."));
            if ((configRevision?.Value ?? CurrentConfigRevision) < 1 && legacySizes.TryGetValue(kind, out var legacy))
                MigrateLegacySize(setting.Config, legacy, defaultSizes[kind]);
            RefreshSetting(kind, setting);
        }

        private static void MigrateLegacySize(ISettingConfigGroup config, (float Width, float Height) legacy, (float Width, float Height) current)
        {
            if (config.Width is IMutableConfigEntry<float> width && Same(width.Value, legacy.Width))
                width.Value = current.Width;
            if (config.Height is IMutableConfigEntry<float> height && Same(height.Value, legacy.Height))
                height.Value = current.Height;
            Dimension2D legacyOffset = ElevationOffset(legacy.Width, legacy.Height);
            if (config.ElevationMarkerOffset is IMutableConfigEntry<Vector2> offset
                && Same(offset.Value.x, legacyOffset.Width) && Same(offset.Value.y, legacyOffset.Height))
            {
                Dimension2D next = ElevationOffset(current.Width, current.Height);
                offset.Value = new Vector2(next.Width, next.Height);
            }

            static bool Same(float a, float b) => Math.Abs(a - b) < 0.001f;
        }

        public static void SetMinimapScale(float zoom)
        {
            if (minimapScale is IMutableConfigEntry<float> mutable)
                mutable.Value = Numeric.Clamp(zoom, 0.1f, 20f, 1f);
        }

        public static void Refresh()
        {
            float width = Numeric.Clamp(mapWidth?.Value ?? 180f, 60f, 800f, 180f);
            float height = Numeric.Clamp(mapHeight?.Value ?? 180f, 60f, 800f, 180f);
            ViewfinderSize.Width = MinimapSize.Width = width;
            ViewfinderSize.Height = MinimapSize.Height = height;
            foreach (KeyValuePair<InteractableKind, InteractibleSetting> pair in InteractibleSettings)
                RefreshSetting(pair.Key, pair.Value);
        }

        private static void RefreshSetting(InteractableKind kind, InteractibleSetting setting)
        {
            ISettingConfigGroup config = setting.Config;
            if (config == null)
                return;

            (float width, float height) = defaultSizes.TryGetValue(kind, out var size) ? size : (11f, 11f);
            setting.Enabled = config.Enabled.Value;
            setting.Dimensions.Width = Numeric.Clamp(config.Width.Value, 0.5f, 128f, width);
            setting.Dimensions.Height = Numeric.Clamp(config.Height.Value, 0.5f, 128f, height);
            setting.ActiveColor = SanitizeColor(config.ActiveColor.Value);
            setting.InactiveColor = SanitizeColor(config.InactiveColor.Value);
            setting.IconPath = string.IsNullOrWhiteSpace(config.IconPath.Value) ? Icons.Default : config.IconPath.Value;
            setting.ElevationMarkerEnabled = config.EnabledElevationMarker?.Value ?? false;
            setting.ElevationMarkerColor = SanitizeColor(config.ElevationMarkerColor?.Value ?? Color.white);
            setting.ElevationMarkerDimensions.Width = Numeric.Clamp(config.ElevationMarkerWidth?.Value ?? 3f, 0.5f, 64f, 3f);
            setting.ElevationMarkerDimensions.Height = Numeric.Clamp(config.ElevationMarkerHeight?.Value ?? 3f, 0.5f, 64f, 3f);
            Vector2 offset = config.ElevationMarkerOffset?.Value ?? Vector2.zero;
            setting.ElevationMarkerOffset.Width = Numeric.Clamp(offset.x, -256f, 256f, 0f);
            setting.ElevationMarkerOffset.Height = Numeric.Clamp(offset.y, -256f, 256f, 0f);
        }

        private static Color SanitizeColor(Color color) => new(
            Numeric.Clamp(color.r, 0f, 1f, 1f), Numeric.Clamp(color.g, 0f, 1f, 1f),
            Numeric.Clamp(color.b, 0f, 1f, 1f), Numeric.Clamp(color.a, 0f, 1f, 1f));
    }
}
