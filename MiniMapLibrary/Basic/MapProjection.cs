using System;
using UnityEngine;

namespace MiniMapLibrary
{
    public static class MapProjection
    {
        // Use one scale on both axes so bearings remain accurate on narrow stages.
        public static float GetScale(Range3D bounds, Dimension2D size, float zoom, float worldRadius = 0f)
        {
            float width = Numeric.Clamp(size.Width, 1f, 4096f, 180f);
            float height = Numeric.Clamp(size.Height, 1f, 4096f, 180f);
            zoom = Numeric.Clamp(zoom, 0.1f, 20f, 1f);

            if (Numeric.IsFinite(worldRadius) && worldRadius > 0f)
                return Math.Min(width, height) * zoom / (2f * Math.Max(worldRadius, 1f));

            float xSpan = Numeric.Clamp(bounds?.X.Difference ?? 0f, 100f, 1000000f, 100f);
            float zSpan = Numeric.Clamp(bounds?.Z.Difference ?? 0f, 100f, 1000000f, 100f);
            return Math.Min(width / xSpan, height / zSpan) * zoom;
        }

        public static Vector2 ProjectOffset(Vector3 position, Vector3 origin, float scale)
        {
            return new Vector2((position.x - origin.x) * scale, (position.z - origin.z) * scale);
        }

        public static bool TryProject(Vector3 position, Vector3 origin, float scale, float sine, float cosine, out Vector2 result)
        {
            result = Vector2.zero;
            if (!Numeric.IsFinite(position.x) || !Numeric.IsFinite(position.y) || !Numeric.IsFinite(position.z)
                || !Numeric.IsFinite(origin.x) || !Numeric.IsFinite(origin.y) || !Numeric.IsFinite(origin.z)
                || !Numeric.IsFinite(scale) || scale <= 0f || !Numeric.IsFinite(sine) || !Numeric.IsFinite(cosine))
                return false;
            result = Rotate(ProjectOffset(position, origin, scale), sine, cosine);
            return Numeric.IsFinite(result.x) && Numeric.IsFinite(result.y);
        }

        public static bool IsInside(Vector2 position, float sine, float cosine, float halfWidth, float halfHeight, float padding)
        {
            return IsInside(Rotate(position, sine, cosine), halfWidth, halfHeight, padding);
        }

        public static Vector2 Rotate(Vector2 position, float sine, float cosine) => new(
            position.x * cosine - position.y * sine, position.x * sine + position.y * cosine);

        public static bool IsInside(Vector2 position, float halfWidth, float halfHeight, float padding) =>
            Numeric.IsFinite(position.x) && Numeric.IsFinite(position.y)
            && Math.Abs(position.x) <= halfWidth + padding && Math.Abs(position.y) <= halfHeight + padding;
    }
}
