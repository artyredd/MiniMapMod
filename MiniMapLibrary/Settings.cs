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
            void AddSize(InteractableKind type, float width = -1, float height = -1, Color ActiveColor = default, Color InactiveColor = default)
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

                InteractibleSettings.Add(type, setting);
            }

            AddSize(InteractableKind.Chest, 10, 8);
            AddSize(InteractableKind.Shrine);
            AddSize(InteractableKind.Teleporter, 15, 15, ActiveColor: Color.white, InactiveColor: Color.green);
            AddSize(InteractableKind.Player, 8, 8, ActiveColor: PlayerIconColor, InactiveColor: PlayerIconColor);
            AddSize(InteractableKind.Barrel, 5, 5);
            AddSize(InteractableKind.Drone, 7, 7);
            AddSize(InteractableKind.Primary);
            AddSize(InteractableKind.Special, 7, 7);
            AddSize(InteractableKind.Enemy, 3, 3, ActiveColor: Color.red);
            AddSize(InteractableKind.Utility);
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
    }
}
