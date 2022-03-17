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
        }

        public static Dimension2D MinimapSize { get; set; } = new Dimension2D(100, 100);

        public static Dimension2D ViewfinderSize { get; set; } = new Dimension2D(10, 100);

        public static IDictionary<InteractableKind, InteractibleSetting> InteractibleSettings { get; } = new Dictionary<InteractableKind, InteractibleSetting>();

        private static readonly Dimension2D DefaultUIElementSize = new Dimension2D(10, 10);

        public static Color PlayerIconColor { get; set; } = Color.white;

        public static Color DefaultActiveColor { get; set; } = Color.yellow;

        public static Color DefaultInactiveColor { get; set; } = Color.grey;

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
                Color ActiveColor = default, 
                Color InactiveColor = default, 
                string description = "",
                string path = Icons.Default)
            {
                ActiveColor = ActiveColor == default ? DefaultActiveColor : ActiveColor;
                InactiveColor = InactiveColor == default ? DefaultInactiveColor : InactiveColor;

                Dimension2D size = DefaultUIElementSize;

                if (width != -1 || height != -1)
                {
                    size = new Dimension2D(width, height);
                }

                var setting = new InteractibleSetting()
                {
                    ActiveColor = ActiveColor,
                    InactiveColor = InactiveColor,
                    Dimensions = size
                };

                setting.Description = description;
                setting.IconPath = path;

                InteractibleSettings.Add(type, setting);
            }

            Add(InteractableKind.Chest, 10, 8, 
                description: "Chests, including shops", 
                path: Icons.Chest);

            Add(InteractableKind.Equipment, 8, 6,
                description: "Equipment barrels",
                path: Icons.Chest);

            Add(InteractableKind.Shrine, 
                description: "All shrines (excluding Newt)", 
                path: Icons.Shrine);
           
            Add(InteractableKind.Teleporter, 15, 15, 
                ActiveColor: Color.white, 
                InactiveColor: Color.green, 
                description: "Boss teleporters",
                path: Icons.Boss);

            Add(InteractableKind.Player, 8, 8, 
                ActiveColor: PlayerIconColor, 
                InactiveColor: PlayerIconColor, 
                description: "Player, including friends",
                path: Icons.Circle);

            Add(InteractableKind.Barrel, 5, 5, 
                description: "Barrels", 
                path: Icons.Circle);

            Add(InteractableKind.Drone, 7, 7, 
                description: "Drones", 
                path: Icons.Drone);

            Add(InteractableKind.Special, 7, 7, 
                description: "Special interactibles such as the landing pod and fans",
                path: Icons.Default);

            Add(InteractableKind.Enemy, 3, 3, 
                ActiveColor: Color.red, 
                description: "Enemies",
                path: Icons.Circle);

            Add(InteractableKind.EnemyVoid, 3, 3,
                ActiveColor: Color.magenta,
                description: "Void touched enemies",
                path: Icons.Circle);

            Add(InteractableKind.Ally, 3, 3,
                ActiveColor: Color.green,
                description: "Allies and minions",
                path: Icons.Circle);

            Add(InteractableKind.Utility, 
                description: "Scrappers", 
                path: Icons.LootBag);

            Add(InteractableKind.Printer, 10, 8, 
                description: "Printers",
                path: Icons.Chest);

            Add(InteractableKind.LunarPod, 7, 7, 
                description: "Lunar pods (chests)",
                path: Icons.LootBag);

            Add(InteractableKind.Shop, 7, 7,
                description: "3 item shops",
                path: Icons.Chest);
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

            setting.Config = new SettingConfigGroup(enabled, height, width, activeColor, inactiveColor, path);

            setting.ActiveColor = activeColor.Value;
            setting.InactiveColor = inactiveColor.Value;
            setting.Dimensions.Height = height.Value;
            setting.Dimensions.Width = width.Value;
            setting.IconPath = path.Value;
        }
    }
}
