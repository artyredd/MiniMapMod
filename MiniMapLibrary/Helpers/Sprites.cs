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
            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var transform = (RectTransform)icon.transform;

            transform.sizeDelta = new Vector2(width, height);

            var image = icon.GetComponent<Image>();

            image.sprite = sprite;

            image.color = color;
            image.raycastTarget = false;

            return icon;
        }
    }
}
