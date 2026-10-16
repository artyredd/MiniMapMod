using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Helpers
{
    public static class Transforms
    {
        public static void SetParent(Transform child, Transform parent)
        {
            child.SetParent(parent, false);
            // Layers aren't inherited, and the game's UI camera doesn't render the Default layer.
            child.gameObject.layer = parent.gameObject.layer;
            child.localRotation = Quaternion.identity;
            child.localPosition = Vector3.zero;
            child.localScale = Vector3.one;
        }
    }
}
