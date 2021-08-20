using MiniMapLibrary;
using UnityEngine;

namespace MiniMapMod
{
    public interface ITrackedObject
    {
        GameObject gameObject { get; set; }
        InteractableKind InteractableType { get; set; }
        RectTransform MinimapTransform { get; set; }
        bool DynamicObject { get; set; }

        void CheckActive();
        void Destroy();
    }
}