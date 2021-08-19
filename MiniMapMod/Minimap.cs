using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMapLibrary
{
    public class Minimap
    {
        public static GameObject gameObject { get; private set; }

        public static GameObject Container { get; private set; }

        private RectTransform ContainerTransform;

        public bool Created { get; private set; } = false;

        public void CreateMinimap(SpriteManager spriteManager, GameObject Parent)
        {
            if (Created)
            {
                return;
            }

            Created = true;

            gameObject = CreateMask();

            gameObject.transform.SetParent(Parent.transform);

            gameObject.transform.localRotation = Quaternion.identity;

            gameObject.transform.localPosition = new Vector3(0, 0, 0);

            gameObject.transform.localScale = new Vector3(1, 1, 1);

            CreatePlayerIcon(spriteManager);

            CreateIconContainer();
        }

        public void Destroy()
        {
            if (Created == false)
            {
                return;
            }

            GameObject.Destroy(gameObject);

            gameObject = null;
            Container = null;
            ContainerTransform = null;

            Created = false;
        }

        public void Zoom(int ZoomLevel)
        {
            if (ContainerTransform != null)
            {
                ZoomLevel = Math.Max(1, ZoomLevel);

                ContainerTransform.localScale = new Vector3(ZoomLevel, ZoomLevel, ZoomLevel);
            }
        }

        public void SetRotation(Quaternion rotation)
        {
            var euler = rotation.eulerAngles;

            ContainerTransform.localRotation = Quaternion.Euler(0, 0, euler.y);
        }

        public RectTransform CreateIcon(InteractableKind type, Vector3 minimapPosition, SpriteManager spriteManager)
        {
            var icon = CreateIcon(type, spriteManager.GetSprite(type));

            var transform = icon.GetComponent<RectTransform>();

            transform.localPosition = minimapPosition;

            transform.SetParent(ContainerTransform);

            transform.localRotation = Quaternion.identity;
            transform.localPosition = new Vector3(0, 0, 0);
            transform.localScale = new Vector3(1, 1, 1);

            return transform;
        }

        private void CreateIconContainer()
        {
            GameObject container = Create(obj =>
            {
                obj.name = "icon_container";

                ContainerTransform = obj.AddComponent<RectTransform>();

                ContainerTransform.sizeDelta = new(Settings.MinimapSize.Width, Settings.MinimapSize.Height);
            });

            container.transform.SetParent(gameObject.transform);

            ContainerTransform.rotation = Quaternion.identity;

            ContainerTransform.localPosition = new Vector3(0, 0, 0);

            ContainerTransform.localScale = new Vector3(1, 1, 1);

            Container = container;
        }

        private void CreatePlayerIcon(SpriteManager spriteManager)
        {
            GameObject playerIcon = CreateIcon(InteractableKind.Player, spriteManager.GetSprite(InteractableKind.Player));

            playerIcon.transform.SetParent(gameObject.transform);

            playerIcon.transform.localPosition = new Vector3(0, 0, 0);
            playerIcon.transform.localScale = new Vector3(1, 1, 1);
        }

        private GameObject CreateIcon(InteractableKind type, Sprite iconTexture)
        {
            return Create(icon =>
            {
                icon.name = type.ToString();

                var transform = icon.AddComponent<RectTransform>();

                Dimension2D size = Settings.GetInteractableSize(type);

                transform.sizeDelta = new Vector2(size.Width, size.Height);

                icon.AddComponent<CanvasRenderer>();
                icon.AddComponent<Image>().sprite = iconTexture;
            });
        }

        private GameObject CreateMask()
        {
            return Create(mask =>
            {
                mask.name = "Minimap";

                RectTransform newTransform = mask.AddComponent<RectTransform>();

                newTransform.sizeDelta = new Vector2(Settings.ViewfinderSize.Width, Settings.ViewfinderSize.Height);

                mask.AddComponent<CanvasRenderer>();
                //mask.AddComponent<Image>();

                mask.AddComponent<Mask>();

                LayoutElement element = mask.AddComponent<LayoutElement>();

                element.minHeight = Settings.MinimapSize.Height;
                element.flexibleHeight = 0;
            });
        }

        private GameObject Create(Action<GameObject> Expression)
        {
            GameObject newObj = new();

            Expression(newObj);

            return newObj;
        }
    }
}
