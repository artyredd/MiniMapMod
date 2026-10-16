using System;

namespace MiniMapLibrary
{
    public static class Numeric
    {
        public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static float Clamp(float value, float minimum, float maximum, float fallback)
        {
            return IsFinite(value) ? Math.Max(minimum, Math.Min(maximum, value)) : fallback;
        }
    }
}
