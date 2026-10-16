using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMapLibrary.Scanner
{
    public class TrackedObject<T> : ITrackedObject
    {
        private RectTransform minimapTransform;
        private Image minimapImage;
        private Color appliedColor;
        private bool hasAppliedColor;
        private bool reportedFailure;

        public GameObject gameObject { get; set; }
        public RectTransform MinimapTransform
        {
            get => minimapTransform;
            set
            {
                if (ReferenceEquals(minimapTransform, value))
                    return;
                minimapTransform = value;
                minimapImage = null;
                hasAppliedColor = false;
            }
        }

        public Image MinimapImage
        {
            get
            {
                if (!minimapImage && minimapTransform)
                    minimapImage = minimapTransform.GetComponent<Image>();
                return minimapImage;
            }
            set { minimapImage = value; hasAppliedColor = false; }
        }

        public bool DynamicObject { get; set; }
        public T BackingObject { get; set; }
        public InteractableKind InteractableType { get; set; }
        public Func<T, bool> ActiveChecker { get; set; }
        public bool Active { get; private set; } = true;
        public ILogger ErrorLogger { get; set; }

        public TrackedObject(InteractableKind interactableType, GameObject gameObject, RectTransform minimapTransform)
        {
            InteractableType = interactableType;
            this.gameObject = gameObject;
            MinimapTransform = minimapTransform;
        }

        public void Destroy()
        {
            if (minimapTransform)
                UnityEngine.Object.Destroy(minimapTransform.gameObject);
            MinimapTransform = null;
        }

        public void CheckActive()
        {
            if (!gameObject || BackingObject == null || (BackingObject is UnityEngine.Object source && !source))
            {
                Active = false;
            }
            else
            {
                try
                {
                    Active = ActiveChecker?.Invoke(BackingObject) ?? true;
                }
                catch (Exception exception)
                {
                    Active = false;
                    if (!reportedFailure)
                    {
                        reportedFailure = true;
                        ErrorLogger?.LogException(exception, $"Availability check failed for {InteractableType}.");
                    }
                }
            }

            ApplyColor();
        }

        public void ApplyColor()
        {
            Image image = MinimapImage;
            if (!image)
                return;
            Color color = Settings.GetColor(InteractableType, Active);
            if (!hasAppliedColor || appliedColor != color)
            {
                image.color = color;
                appliedColor = color;
                hasAppliedColor = true;
            }
        }
    }
}
