using MiniMapLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapMod
{
    public class TrackedObject
    {
        public GameObject gameObject { get; set; }
        public RectTransform MinimapTransform { get; set; }
        public InteractableKind InteractableType { get; set; }

        public TrackedObject(InteractableKind interactableType, GameObject gameObject, RectTransform minimapTransform)
        {
            this.gameObject = gameObject;
            MinimapTransform = minimapTransform;
            InteractableType = interactableType;
        }
    }
}
