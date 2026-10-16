using System;
using MiniMapLibrary;
using UnityEngine;

#nullable enable
namespace MiniMapMod
{
    public static class MinimapExtensions
    {
        public static Vector2 ToMinimapPosition(this Vector3 position, Range3D dimensions)
        {
            float scale = MapProjection.GetScale(dimensions, Settings.MinimapSize, 1f);
            return new Vector2((position.x + dimensions.X.Offset) * scale, (position.z + dimensions.Z.Offset) * scale);
        }

        public static T? Check<T>(this T value, Func<T, bool> expression) where T : class =>
            expression?.Invoke(value) == true ? value : null;
    }
}
