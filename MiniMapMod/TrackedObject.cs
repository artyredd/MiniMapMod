using MiniMapLibrary;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMapMod
{
    public class TrackedObject<T> : ITrackedObject
    {
        public GameObject gameObject { get; set; }

        public RectTransform MinimapTransform { get; set; }

        public bool DynamicObject { get; set; } = false;

        public T BackingObject { get; set; }

        public Image MinimapImage
        {
            get
            {
                if (_MinimapImage is null && MinimapTransform != null)
                {
                    _MinimapImage = MinimapTransform.GetComponent<Image>();
                }

                return _MinimapImage;
            }
            set => _MinimapImage = value;
        }

        private Image _MinimapImage;

        public InteractableKind InteractableType { get; set; }

        public Func<T, bool> ActiveChecker { get; set; }

        public bool Active { get; private set; } = true;

        private bool PreviousActive = true;

        public void Destroy()
        {
            try
            {
                GameObject.Destroy(MinimapTransform.gameObject);
            }
            catch (Exception)
            {

            }
        }

        public void CheckActive()
        {
            if (ActiveChecker != null && BackingObject != null)
            {
                try
                {
                    Active = ActiveChecker(BackingObject);
                }
                catch (Exception)
                {
                    Active = false;
                }

                if (PreviousActive != Active)
                {
                    PreviousActive = Active;

                    MinimapImage.color = Settings.GetColor(InteractableType, Active);
                }
            }
        }

        public TrackedObject(InteractableKind interactableType, GameObject gameObject, RectTransform minimapTransform)
        {
            this.gameObject = gameObject;
            MinimapTransform = minimapTransform;
            InteractableType = interactableType;
        }
    }
}
