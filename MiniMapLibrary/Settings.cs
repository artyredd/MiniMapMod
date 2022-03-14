using MiniMapLibrary.Interfaces;
using MiniMapMod.wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MiniMapLibrary
{
    public static class Settings
    {
        public static Dimension2D MinimapSize { get; set; } = new Dimension2D(100, 100);

        public static Dimension2D ViewfinderSize { get; set; } = new Dimension2D(10, 100);

        public static IDictionary<InteractableKind, InteractibleSetting> InteractibleSettings { get; } = new Dictionary<InteractableKind, InteractibleSetting>();

        private static readonly Dimension2D DefaultUIElementSize = new Dimension2D(10, 10);

        public static Color PlayerIconColor { get; set; } = Color.white;

        public static Color DefaultActiveColor { get; set; } = Color.yellow;

        public static Color DefaultInactiveColor { get; set; } = Color.grey;

        static Settings()
        {
            InitializeDefaultSettings();
        }

        private static void InitializeDefaultSettings()
        {
            static void AddSize(InteractableKind type, float width = -1, float height = -1, Color ActiveColor = default, Color InactiveColor = default, string description = "")
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

                InteractibleSettings.Add(type, setting);
            }

            AddSize(InteractableKind.Chest, 10, 8, description: "Chests, including shops");
            AddSize(InteractableKind.Shrine, description: "All shrines (excluding Newt)");
            AddSize(InteractableKind.Teleporter, 15, 15, ActiveColor: Color.white, InactiveColor: Color.green, description: "Boss teleporters");
            AddSize(InteractableKind.Player, 8, 8, ActiveColor: PlayerIconColor, InactiveColor: PlayerIconColor, description: "");
            AddSize(InteractableKind.Barrel, 5, 5, description: "Barrels");
            AddSize(InteractableKind.Drone, 7, 7, description: "Drones");
            AddSize(InteractableKind.Special, 7, 7, description: "Special interactibles such as the landing pod");
            AddSize(InteractableKind.Enemy, 3, 3, ActiveColor: Color.red, description: "Enemies");
            AddSize(InteractableKind.Utility, description: "Scrappers");
            AddSize(InteractableKind.Printer, 10, 8, description: "Printers");
            AddSize(InteractableKind.LunarPod, 7, 7, description: "Lunar pods (chests)");
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
                InactiveColor = DefaultInactiveColor
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
                return InteractibleSettings[type].Dimensions ?? DefaultUIElementSize;
            }

            return DefaultUIElementSize;
        }

        public static void LoadConfigEntries(InteractableKind type, IConfig config)
        {
            InteractibleSetting setting = InteractibleSettings[type];

            IConfigEntry<bool> enabled = config.Bind<bool>($"Icon.{type}", "enabled", true, $"Whether or not {setting.Description} should be shown on the minimap");
            IConfigEntry<Color> activeColor = config.Bind<Color>($"Icon.{type}", "activeColor", setting.ActiveColor, "The color the icon should be when it has not been interacted with");
            IConfigEntry<Color> inactiveColor = config.Bind<Color>($"Icon.{type}", "inactiveColor", setting.InactiveColor, "The color the icon should be when it has used/bought");
            IConfigEntry<float> width = config.Bind<float>($"Icon.{type}", "width", setting.Dimensions.Width, "Width of the icon");
            IConfigEntry<float> height = config.Bind<float>($"Icon.{type}", "height", setting.Dimensions.Height, "Width of the icon");


            InteractibleSettings[type].Config = new SettingConfigGroup(enabled, height, width, activeColor, inactiveColor);

            setting.ActiveColor = activeColor.Value;
            setting.InactiveColor = inactiveColor.Value;
            setting.Dimensions.Height = height.Value;
            setting.Dimensions.Width = width.Value;
        }

        public static void UpdateSetting(InteractableKind type, float width, float height, Color active, Color inactive)
        {
            if (InteractibleSettings.ContainsKey(type))
            {
                var setting = InteractibleSettings[type];

                setting.ActiveColor = active;
                setting.InactiveColor = inactive;
                setting.Dimensions.Height = height;
                setting.Dimensions.Width = width;
            }
        }
    }
}
