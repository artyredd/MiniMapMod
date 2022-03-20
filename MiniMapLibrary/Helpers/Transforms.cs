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
            child.transform.SetParent(parent);

            child.transform.localRotation = Quaternion.identity;

            child.transform.localPosition = new Vector3(0, 0, 0);

            child.transform.localScale = new Vector3(1, 1, 1);
        }
    }
}
