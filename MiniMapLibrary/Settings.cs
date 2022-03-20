using MiniMapLibrary.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniMapLibrary
{
    public enum LogLevel { 
        none,
        info,
        debug,
        all
    }

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
        }

        public static class Colors
        {
            public static Color Yellow = new Color(255/255.0f, 219 / 255.0f, 88 / 255.0f);
            public static Color Purple = new Color(122 / 255.0f, 105 / 255.0f, 244 / 255.0f);
            public static Color DarkPurple = new Color(76 / 255.0f, 53 / 255.0f, 244 / 255.0f);
            public static Color Teal = new Color(94 / 255.0f, 178 / 255.0f, 242 / 255.0f);
            public static Color Pink = new Color(172 / 255.0f, 95 / 255.0f, 243 / 255.0f);
            public static Color DarkPink = new Color(146 / 255.0f, 39 / 255.0f, 243 / 255.0f);
            public static Color Orange = new Color(255 / 255.0f, 146 / 255.0f, 0 / 255.0f);
            public static Color DarkTeal = new Color(94 / 255.0f, 178 / 255.0f, 242 / 255.0f);
        }

        public static Dimension2D MinimapSize { get; set; } = new Dimension2D(100, 100);

        public static Dimension2D ViewfinderSize { get; set; } = new Dimension2D(10, 100);

        public static IDictionary<InteractableKind, InteractibleSetting> InteractibleSettings { get; } = new Dictionary<InteractableKind, InteractibleSetting>();

        private static readonly Dimension2D DefaultUIElementSize = new Dimension2D(10, 10);

        public static Color PlayerIconColor { get; set; } = Color.white;

        public static Color DefaultActiveColor { get; set; } = Colors.Yellow;

        public static Color DefaultInactiveColor { get; set; } = Color.grey;

        public static Color DefaultElevationMarkerColor { get; set; } = Color.white;

        public static Dimension2D DefaultElevationSize { get; set; } = new Dimension2D(3.0f, 3.0f);

        public static LogLevel LogLevel => _logLevel.Value;

        private static IConfigEntry<LogLevel> _logLevel;

        private static ILogger logger;

        static Settings()
        {
            InitializeDefaultSettings();
        }

        private static void InitializeDefaultSettings()
        {
            static void Add(InteractableKind type, 
                float width = -1, 
                float height = -1, 
                Color activeColor = default, 
                Color inactiveColor = default, 
                string description = "",
                string path = Icons.Default,
                Color elevationMarkerColor = default
            )
            {
                activeColor = activeColor == default ? DefaultActiveColor : activeColor;
                inactiveColor = inactiveColor == default ? DefaultInactiveColor : inactiveColor;
                elevationMarkerColor = elevationMarkerColor == default ? DefaultElevationMarkerColor : elevationMarkerColor;

                Dimension2D size = DefaultUIElementSize;

                if (width != -1 || height != -1)
                {
                    size = new Dimension2D(width, height);
                }

                var setting = new InteractibleSetting()
                {
                    ActiveColor = activeColor,
                    InactiveColor = inactiveColor,
                    Dimensions = size
                };

                setting.Description = description;
                setting.IconPath = path;

                setting.ElevationMarkerColor = elevationMarkerColor;
                setting.ElevationMarkerEnabled = true;
                setting.ElevationMarkerDimensions = new Dimension2D(DefaultElevationSize.Width, DefaultElevationSize.Height);
                setting.ElevationMarkerOffset = new Dimension2D(
                    (size.Width / 2.0f) + (DefaultElevationSize.Width / 2.0f), 
                    (size.Height / 2.0f) + (DefaultElevationSize.Height / 2.0f)
                );

                InteractibleSettings.Add(type, setting);
            }

            Add(InteractableKind.Chest, 8, 6,
                description: "Chests, Roulette Chests",
                path: Icons.Chest);

            Add(InteractableKind.Shop, 7, 5,
                activeColor: Colors.Teal,
                description: "Shops",
                path: Icons.Chest);

            Add(InteractableKind.Equipment, 8, 6,
                activeColor: Colors.Orange,
                description: "Equipment Barrels",
                path: Icons.Chest);

            Add(InteractableKind.Printer, 10, 8,
                activeColor: Colors.Purple,
                description: "Printers",
                path: Icons.Chest);

            Add(InteractableKind.Utility, 5, 5,
                description: "Scrappers",
                path: Icons.Wrench);

            Add(InteractableKind.LunarPod, 7, 7,
                description: "Lunar pods (chests)",
                activeColor: Color.cyan,
                path: Icons.LootBag);

            Add(InteractableKind.Shrine,
                description: "All shrines (excluding Newt)",
                path: Icons.Shrine);
           
            Add(InteractableKind.Teleporter, 15, 15,
                activeColor: Color.white,
                inactiveColor: Color.green,
                description: "Boss teleporters",
                path: Icons.Boss);

            Add(InteractableKind.Barrel, 3, 3,
                description: "Barrels",
                path: Icons.Circle);

            Add(InteractableKind.Drone, 7, 7,
                description: "Drones",
                path: Icons.Drone);

            Add(InteractableKind.Special, 5, 5,
                description: "Special interactibles such as the landing pod and fans",
                path: Icons.Default);

            Add(InteractableKind.EnemyMonster, 3, 3,
                activeColor: Color.red,
                elevationMarkerColor: Color.red,
                description: "Enemies",
                path: Icons.Circle);

            Add(InteractableKind.EnemyLunar, 3, 3,
                activeColor: Color.red,
                elevationMarkerColor: Color.red,
                description: "Lunar enemies",
                path: Icons.Circle);

            Add(InteractableKind.EnemyVoid, 12, 12,
                activeColor: Color.magenta,
                elevationMarkerColor: Color.magenta,
                description: "Void touched enemies",
                path: Icons.Attack);

            Add(InteractableKind.Minion, 3, 3,
                activeColor: Color.green,
                elevationMarkerColor: Color.green,
                description: "Minions",
                path: Icons.Circle);

            Add(InteractableKind.Player, 8, 8,
                activeColor: PlayerIconColor,
                inactiveColor: PlayerIconColor,
                description: "Player, including friends",
                path: Icons.Sprint);

            Add(InteractableKind.Item, 3, 3,
                activeColor: Colors.Teal,
                inactiveColor: Color.cyan,
                description: "Dropped items and lunar coins",
                path: Icons.Circle);

            Add(InteractableKind.Portal, 7, 7,
               activeColor: DefaultActiveColor,
               inactiveColor: DefaultInactiveColor,
               description: "Portal markers, or objects that spawn portals when interacted with (excluding newt altar)",
               path: Icons.Portal);

            Add(InteractableKind.Totem, 7, 7,
               activeColor: Colors.DarkTeal,
               inactiveColor: DefaultInactiveColor,
               description: "Totems, or totem adjacent objects",
               path: Icons.Cube);

            Add(InteractableKind.Neutral, 4, 4,
               activeColor: Color.white,
               inactiveColor: DefaultInactiveColor,
               description: "Neutral objects or NPCs",
               path: Icons.Circle);
        }

        public static InteractibleSetting GetSetting(InteractableKind type)
        {
            if (InteractibleSettings.ContainsKey(type))
            {
                return InteractibleSettings[type];
            }

            return new InteractibleSetting()
            {
                Dimensions = DefaultUIElementSize,
                ActiveColor = DefaultActiveColor,
                InactiveColor = DefaultInactiveColor,
                Description = "NO_DESCRIPTION",
                IconPath = Icons.Default
            };
        }

        public static Color GetColor(InteractableKind type, bool active)
        {
            if (InteractibleSettings.ContainsKey(type))
            {
                var setting = InteractibleSettings[type];

                return active ? setting.ActiveColor : setting.InactiveColor;
            }

            return active ? DefaultActiveColor : DefaultInactiveColor;
        }

        public static Dimension2D GetInteractableSize(InteractableKind type)
        {
            if (InteractibleSettings.ContainsKey(type))
            {
                return InteractibleSettings[type]?.Dimensions ?? DefaultUIElementSize;
            }

            return DefaultUIElementSize;
        }

        public static void LoadApplicationSettings(ILogger logger, IConfig config) 
        {
            Settings.logger = logger;
            _logLevel = config.Bind<LogLevel>($"Settings.General", "LogLevel", LogLevel.info, "The amount of information that the minimap mod should output to the console during runtime");
            logger.LogDebug($"Loaded log level: {Settings.LogLevel}");
        }

        public static void LoadConfigEntries(InteractableKind type, IConfig config)
        {
            InteractibleSetting setting;

            if (InteractibleSettings.ContainsKey(type) is false)
            {
                logger.LogError($"Failed to find an interactible setting for {type}, aborting config loading for {type}s");
                return;
            }

            setting = InteractibleSettings[type];

            string sectionTitle = $"Icon.{type}";

            IConfigEntry<bool> enabled = config.Bind<bool>(sectionTitle, "enabled", true, $"Whether or not {setting.Description} should be shown on the minimap");
            IConfigEntry<Color> activeColor = config.Bind<Color>(sectionTitle, "activeColor", setting.ActiveColor, "The color the icon should be when it has not been interacted with");
            IConfigEntry<Color> inactiveColor = config.Bind<Color>(sectionTitle, "inactiveColor", setting.InactiveColor, "The color the icon should be when it has used/bought");
            IConfigEntry<float> width = config.Bind<float>(sectionTitle, "width", setting.Dimensions.Width, "Width of the icon");
            IConfigEntry<float> height = config.Bind<float>(sectionTitle, "height", setting.Dimensions.Height, "Width of the icon");
            IConfigEntry<string> path = config.Bind<string>(sectionTitle, "icon", setting.IconPath ?? Icons.Default, $"The streaming assets path of the icon");

            IConfigEntry<bool> elevationEnabled = config.Bind<bool>(sectionTitle, "elevationMarkerEnabled", true, $"Whether or not the elevation marker for this icon should be shown on the minimap");
            IConfigEntry<Color> elevationActiveColor = config.Bind<Color>(sectionTitle, "elevationMarkerColor", setting.ElevationMarkerColor, "The color the elevation marker should be when it is shown");
            IConfigEntry<float> elevationWidth = config.Bind<float>(sectionTitle, "elevationMarkerWidth", setting.ElevationMarkerDimensions.Width, "Width of the elevation marker icon");
            IConfigEntry<float> elevationHeight = config.Bind<float>(sectionTitle, "elevationMarkerHeight", setting.ElevationMarkerDimensions.Height, "Width of the elevation marker  icon");
            IConfigEntry<Vector2> elevationOffset = config.Bind<Vector2>(sectionTitle, "elevationMarkerOffset", new Vector2(setting.ElevationMarkerOffset.Width, setting.ElevationMarkerOffset.Height), "The value of the x and y of the elevation marker's position relative to the main icon's center");

            setting.Config = new SettingConfigGroup(
                enabled, 
                height, 
                width, 
                activeColor, 
                inactiveColor, 
                path,
                elevationEnabled,
                elevationOffset,
                elevationHeight,
                elevationWidth,
                elevationActiveColor
            );

            setting.ActiveColor = activeColor.Value;
            setting.InactiveColor = inactiveColor.Value;
            setting.Dimensions.Height = height.Value;
            setting.Dimensions.Width = width.Value;
            setting.IconPath = path.Value;
            setting.ElevationMarkerColor = elevationActiveColor.Value;
            setting.ElevationMarkerEnabled = elevationEnabled.Value;
            setting.ElevationMarkerOffset = new Dimension2D(elevationOffset.Value.x, elevationOffset.Value.y);
            setting.ElevationMarkerDimensions = new Dimension2D(elevationWidth.Value, elevationHeight.Value);

            logger.LogInfo($"Loaded {type} config [{(setting.Config.Enabled.Value ? "enabled" : "disabled")}, {setting.ActiveColor}, {setting.InactiveColor}, ({setting.Dimensions.Width}x{setting.Dimensions.Height})] Elevation [{(setting.Config.EnabledElevationMarker.Value ? "enabled" : "disabled")}, {setting.ElevationMarkerColor}, dimensions: ({setting.ElevationMarkerDimensions.Width}x{setting.ElevationMarkerDimensions.Height}), offset: ({setting.ElevationMarkerOffset.Width}x{setting.ElevationMarkerOffset.Height})]");
        }
    }
}
