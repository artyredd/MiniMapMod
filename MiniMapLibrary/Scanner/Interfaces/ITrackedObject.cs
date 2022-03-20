using MiniMapLibrary;
using UnityEngine;

namespace MiniMapLibrary.Scanner
{
    public interface ITrackedObject
    {
        // ignore naming violation, name was chosen because unity style uses the same name
#pragma warning disable IDE1006
        GameObject gameObject { get; set; }
#pragma warning restore IDE1006
        InteractableKind InteractableType { get; set; }
        RectTransform MinimapTransform { get; set; }
        bool DynamicObject { get; set; }
        bool Active { get; }

        void CheckActive();
        void Destroy();
    }
}