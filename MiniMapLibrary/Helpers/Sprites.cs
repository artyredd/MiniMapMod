using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMapLibrary.Helpers
{
    public static class Sprites
    {
        public static GameObject CreateIcon(Sprite sprite, float width, float height, Color color)
        {
            GameObject icon = new GameObject();

            var transform = icon.AddComponent<RectTransform>();

            transform.sizeDelta = new Vector2(width, height);

            icon.AddComponent<CanvasRenderer>();

            var image = icon.AddComponent<Image>();

            image.sprite = sprite;

            image.color = color;

            return icon;
        }
    }
}
