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

        public static IDictionary<InteractableKind, Dimension2D> InteractableSizes { get; } = new Dictionary<InteractableKind, Dimension2D>();

        private static readonly Dimension2D DefaultUIElementSize = new Dimension2D(10, 10);

        public static Color PlayerIconColor { get; set; } = Color.white;

        public static Color DefaultIconColor { get; set; } = Color.yellow;

        static Settings()
        {
            InitializeDefaultSettings();
        }

        private static void InitializeDefaultSettings()
        {
            void AddSize(InteractableKind type, float width = -1, float height = -1)
            {
                Dimension2D size = DefaultUIElementSize;

                if (width != -1 || height != -1)
                {
                    size = new Dimension2D(width, height);
                }

                InteractableSizes.Add(type, size);
            }

            AddSize(InteractableKind.Chest);
            AddSize(InteractableKind.Shrine);
            AddSize(InteractableKind.Teleporter);
            AddSize(InteractableKind.Player);
            AddSize(InteractableKind.Barrel, 5, 5);
            AddSize(InteractableKind.Drone);
            AddSize(InteractableKind.Primary);
            AddSize(InteractableKind.Special);
            AddSize(InteractableKind.Utility);
        }

        public static Dimension2D GetInteractableSize(InteractableKind type)
        {
            if (InteractableSizes.ContainsKey(type))
            {
                return InteractableSizes[type];
            }

            return DefaultUIElementSize;
        }
    }
}
