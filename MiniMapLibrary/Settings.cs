using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniMapLibrary
{
    public static class Settings
    {
        public static Dimension2D MinimapSize { get; set; } = new Dimension2D(100, 100);

        public static Dimension2D ViewfinderSize { get; set; } = new Dimension2D(10, 100);

        public static IDictionary<string, Interactable> InteractableDefinitions { get; } = new Dictionary<string, Interactable>();

        public static IDictionary<InteractableKind, Dimension2D> InteractableSizes { get; } = new Dictionary<InteractableKind, Dimension2D>();

        private static readonly Dimension2D DefaultUIElementSize = new Dimension2D(10, 10);

        static Settings()
        {
            InitializeDefaultSettings();
        }

        private static void InitializeDefaultSettings()
        {
            void Add(string key, InteractableKind type)
            {
                InteractableDefinitions.Add(key, new Interactable(key, type));
            }

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
            AddSize(InteractableKind.Primary);
            AddSize(InteractableKind.Special);
            AddSize(InteractableKind.Utility);

            Add("ShrineCombat", InteractableKind.Shrine);
            Add("ShrineBlood", InteractableKind.Shrine);
            Add("ShrineChance", InteractableKind.Shrine);
            Add("ShrineBoss", InteractableKind.Shrine);

            Add("Teleporter", InteractableKind.Teleporter);

            Add("GoldshoresBeacon", InteractableKind.Special);
            Add("NullSafeZone", InteractableKind.Special);

            Add("TripleShop", InteractableKind.Chest);
            Add("LunarChest", InteractableKind.Chest);
            Add("Chest", InteractableKind.Chest);
            Add("Duplicator", InteractableKind.Chest);
            Add("EquipmentBarrel", InteractableKind.Chest);
            Add("GoldChest", InteractableKind.Chest);
            Add("CategoryChestDamage", InteractableKind.Chest);
            Add("CategoryChestHealing", InteractableKind.Chest);
            Add("CategoryChestUtility", InteractableKind.Chest);

            Add("Scrapper", InteractableKind.Utility);
            Add("NewtStatue", InteractableKind.Utility);
            Add("Barrel", InteractableKind.Utility);
            Add("Drone1Broken", InteractableKind.Utility);
            Add("MissleDroneBroken", InteractableKind.Utility);
            Add("EquipmentDroneBroken", InteractableKind.Utility);
        }

        public static InteractableKind GetInteractableType(string Name)
        {

            if (InteractableDefinitions.ContainsKey(Name))
            {
                return InteractableDefinitions[Name].InteractableType;
            }

            return InteractableKind.none;
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
