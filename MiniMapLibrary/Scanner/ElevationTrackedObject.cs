using System;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMapLibrary.Scanner
{
    public class ElevationTrackedObject : ITrackedObject
    {
        private readonly ITrackedObject backing;
        private readonly ISpriteManager spriteManager;
        private readonly Func<float> heightRetriever;
        private readonly InteractibleSetting setting;
        private Transform arrowTransform;
        private Image arrowImage;
        private int direction;

        public ElevationTrackedObject(ITrackedObject backing, ISpriteManager spriteManager, Func<float> heightRetriever)
        {
            this.backing = backing ?? throw new ArgumentNullException(nameof(backing));
            this.spriteManager = spriteManager ?? throw new ArgumentNullException(nameof(spriteManager));
            this.heightRetriever = heightRetriever ?? throw new ArgumentNullException(nameof(heightRetriever));
            setting = Settings.GetSetting(backing.InteractableType);
        }

        public GameObject gameObject { get => backing.gameObject; set => backing.gameObject = value; }
        public InteractableKind InteractableType { get => backing.InteractableType; set => backing.InteractableType = value; }
        public bool DynamicObject { get => backing.DynamicObject; set => backing.DynamicObject = value; }
        public bool Active => backing.Active;
        public RectTransform MinimapTransform
        {
            get => backing.MinimapTransform;
            set
            {
                if (ReferenceEquals(backing.MinimapTransform, value))
                    return;
                arrowTransform = null;
                arrowImage = null;
                direction = 0;
                backing.MinimapTransform = value;
            }
        }

        public void CheckActive()
        {
            backing.CheckActive();
            if (gameObject)
                UpdateElevation(heightRetriever(), gameObject.transform.position.y, Settings.ElevationThreshold);
        }

        public void UpdateElevation(float viewerHeight, float objectHeight, float threshold)
        {
            if (!MinimapTransform || !setting.ElevationMarkerEnabled)
                return;

            int nextDirection = !Active ? 0 : objectHeight > viewerHeight + threshold ? 1 : objectHeight < viewerHeight - threshold ? -1 : 0;
            if (!arrowTransform)
            {
                // Reuse the arrow when its icon has returned from the pool.
                arrowTransform = MinimapTransform.Find("Elevation");
                if (!arrowTransform && nextDirection == 0)
                    return;
                if (!arrowTransform)
                {
                    GameObject arrow = Helpers.Sprites.CreateIcon(spriteManager.GetOrCache(Settings.Icons.Arrow),
                        setting.ElevationMarkerDimensions.Width, setting.ElevationMarkerDimensions.Height, setting.ElevationMarkerColor);
                    arrow.name = "Elevation";
                    Helpers.Transforms.SetParent(arrow.transform, MinimapTransform);
                    arrow.transform.localPosition = new Vector3(setting.ElevationMarkerOffset.Width, setting.ElevationMarkerOffset.Height, 0f);
                    arrowTransform = arrow.transform;
                }
                arrowImage = arrowTransform.GetComponent<Image>();
                direction = int.MinValue;
            }

            if (direction == nextDirection)
                return;
            direction = nextDirection;
            arrowTransform.gameObject.SetActive(direction != 0);
            if (direction != 0)
                arrowTransform.localRotation = Quaternion.Euler(0f, 0f, direction > 0 ? -90f : 90f);
        }

        public void Destroy()
        {
            backing.Destroy();
            arrowTransform = null;
            arrowImage = null;
            direction = 0;
        }

        public void RefreshSprite()
        {
            if (arrowImage)
                arrowImage.sprite = spriteManager.GetOrCache(Settings.Icons.Arrow);
        }
    }
}
