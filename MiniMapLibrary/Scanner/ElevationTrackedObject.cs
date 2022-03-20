using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Scanner
{
    public class ElevationTrackedObject : ITrackedObject
    {
        private const float margin = 5.0f;

        private readonly ITrackedObject backing;
        private readonly ISpriteManager spriteManager;
        private readonly Func<float> heightRetriever;
        private static readonly Quaternion upDirection;
        private static readonly Quaternion downDirection;

        private Transform arrowTransform;
        private GameObject arrow;

        private bool flag_initialized;
        private bool flag_disabled;

        static ElevationTrackedObject() {
            upDirection = Quaternion.AngleAxis(-90.0f, Vector3.forward);
            downDirection = Quaternion.AngleAxis(90.0f, Vector3.forward);
        }

        public ElevationTrackedObject(ITrackedObject backing, ISpriteManager spriteManager, Func<float> heightRetriever)
        {
            this.backing = backing;
            this.spriteManager = spriteManager;
            this.heightRetriever = heightRetriever;
        }

        public GameObject gameObject { get => backing.gameObject; set => backing.gameObject = value; }

        public InteractableKind InteractableType { get => backing.InteractableType; set => backing.InteractableType = value; }

        public RectTransform MinimapTransform { get => backing.MinimapTransform; set => backing.MinimapTransform = value; }

        public bool DynamicObject { get => backing.DynamicObject; set => backing.DynamicObject = value; }

        public bool Active => backing.Active;

        private bool Initialized()
        {
            // minimap transform, is lazily loaded by minimap at runtime and isn't available
            // until first update call after the scan that produced this object was created
            if (backing.MinimapTransform is null)
            {
                return false;
            }

            // only run this method once
            if (flag_initialized)
            {
                return true;
            }

            // rotate down
            InteractibleSetting setting = Settings.GetSetting(backing.InteractableType);

            if (setting.Config.EnabledElevationMarker.Value is false)
            {
                flag_disabled = true;
                return false;
            }

            Sprite arrowSprite = spriteManager.GetOrCache(Settings.Icons.Arrow);

            arrow = Helpers.Sprites.CreateIcon(arrowSprite, 
                setting.Config.ElevationMarkerWidth.Value, 
                setting.Config.ElevationMarkerHeight.Value, 
                setting.Config.ElevationMarkerColor.Value);

            // attach arrow
            Helpers.Transforms.SetParent(arrow.transform, backing.MinimapTransform);

            arrowTransform = arrow.transform;

            // move arrow
            arrowTransform.localPosition += new Vector3(setting.Config.ElevationMarkerOffset.Value.x, setting.Config.ElevationMarkerOffset.Value.y, 0);

            // rotate arrow upright
            arrowTransform.localRotation = upDirection;

            // hide arrow
            arrow.SetActive(false);

            flag_initialized = true;

            return true;
        }

        public void CheckActive() 
        {
            // check color from backing object
            backing.CheckActive();

            // if the config does not allow arrows don't bother trying to update it, it doesn't exist
            if (flag_disabled)
            {
                return;
            }

            if (Initialized())
            {
                // check if we need to show an arrow denoting height
                if (backing.Active)
                {
                    // since the object is active
                    // show arrow if player is too smol
                    float y = heightRetriever();

                    if (y > (backing.gameObject.transform.position.y + margin))
                    {
                        // item is below player show down arrow
                        arrow.SetActive(true);

                        arrowTransform.localRotation = downDirection;
                    }
                    else if (y < (backing.gameObject.transform.position.y - margin))
                    {
                        // item is above player show up arrow
                        arrow.SetActive(true);

                        arrowTransform.localRotation = upDirection;
                    }
                    else
                    {
                        arrow.SetActive(false);
                    }
                }
                // hide the arrow when the object is no longer active
                else if (arrow.activeSelf)
                {
                    arrow.SetActive(false);
                }
            }
        }

        public void Destroy() => backing.Destroy();
    }
}
