using System;
using MiniMapLibrary;

namespace MiniMapMod
{
    // Context tokens checked against the game's English language files.
    internal static class PurchaseTokenClassifier
    {
        public static InteractableKind Classify(string token, bool isShrine = false, bool isDrone = false)
        {
            token ??= string.Empty;
            if (Contains(token, "STEALTH") && Contains(token, "CHEST"))
                return InteractableKind.StealthChest;
            if (Contains(token, "TIMEDCHEST"))
                return InteractableKind.TimedChest;
            if (token == "LUNAR_CHEST_CONTEXT")
                return InteractableKind.LunarPod;
            if (token == "SHRINE_GOLDSHORES_CONTEXT")
                return InteractableKind.Portal;
            if (token == "GOLDTOTEM_CONTEXT")
                return InteractableKind.Totem;
            if (Contains(token, "DUPLICATOR"))
                return InteractableKind.Printer;
            if (Contains(token, "SCRAPPER") || token == "SHRINE_CLEANSE_CONTEXT"
                || token == "DRONE_ASSEMBLY_CONTEXT" || token == "DRONE_COMBINER_CONTEXT")
                return InteractableKind.Utility;
            if (Contains(token, "EQUIPMENTBARREL"))
                return InteractableKind.Equipment;
            if (Contains(token, "TERMINAL"))
                return InteractableKind.Shop;
            if (Contains(token, "CHEST") || token == "HOLLOWROULETTE_CONTEXT")
                return InteractableKind.Chest;
            if (Contains(token, "PORTAL") || token == "TRIALGONG_CONTEXT")
                return InteractableKind.Portal;
            if (isShrine || Contains(token, "SHRINE") || token.StartsWith("HALLOWEDCAULDRON_", StringComparison.Ordinal))
                return InteractableKind.Shrine;
            if (isDrone)
                return InteractableKind.Drone;
            return InteractableKind.Special;
        }

        public static bool TimedChestAvailable(bool purchased, float lockTime, float stopwatch)
        {
            // The game truncates the remaining time to an int before comparing.
            return !purchased && Numeric.IsFinite(lockTime) && Numeric.IsFinite(stopwatch)
                && (int)(lockTime - stopwatch) > 0;
        }

        private static bool Contains(string token, string part) => token.IndexOf(part, StringComparison.Ordinal) >= 0;
    }
}
