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

        public void CreateMinimap(ISpriteManager spriteManager, GameObject Parent)
        {
            if (Created)
            {
                return;
            }

            Created = true;

            gameObject = CreateMask();

            SetParent(gameObject, Parent);

            CreateIconContainer();

            CreatePlayerIcon(spriteManager);
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

        public void DestroyIcons()
        {
            foreach (Transform child in Container.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        public void SetRotation(Quaternion rotation)
        {
            var euler = rotation.eulerAngles;

            ContainerTransform.localRotation = Quaternion.Euler(0, 0, euler.y);
        }

        public RectTransform CreateIcon(InteractableKind type, Vector3 minimapPosition, ISpriteManager spriteManager)
        {
            var icon = CreateIcon(type, spriteManager.GetSprite(type));

            var transform = icon.GetComponent<RectTransform>();

            transform.localPosition = minimapPosition;

            SetParent(transform, ContainerTransform);

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

            SetParent(container, gameObject);

            Container = container;
        }

        private void CreatePlayerIcon(ISpriteManager spriteManager)
        {
            GameObject playerIcon = CreateIcon(InteractableKind.Player, spriteManager.GetSprite(InteractableKind.Player));

            playerIcon.GetComponent<Image>().color = Settings.PlayerIconColor;

            SetParent(playerIcon, gameObject);
        }

        private GameObject CreateIcon(InteractableKind type, Sprite iconTexture)
        {
            return Create(icon =>
            {
                icon.name = type.ToString();

                var transform = icon.AddComponent<RectTransform>();

                InteractibleSetting settings = Settings.GetSetting(type);

                transform.sizeDelta = new Vector2(settings.Dimensions.Width, settings.Dimensions.Height);

                icon.AddComponent<CanvasRenderer>();

                var image = icon.AddComponent<Image>();

                image.sprite = iconTexture;

                image.color = settings.ActiveColor;
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

                mask.AddComponent<Image>();

                var maskComponent = mask.AddComponent<Mask>();

                maskComponent.showMaskGraphic = false;

                LayoutElement element = mask.AddComponent<LayoutElement>();

                element.minHeight = Settings.MinimapSize.Height;
                element.flexibleHeight = 0;
            });
        }

        private GameObject SetParent(GameObject child, GameObject parent)
        {
            SetParent(child.transform, parent.transform);

            return child;
        }

        private void SetParent(Transform child, Transform parent)
        {
            child.transform.SetParent(parent);

            child.transform.localRotation = Quaternion.identity;

            child.transform.localPosition = new Vector3(0, 0, 0);

            child.transform.localScale = new Vector3(1, 1, 1);
        }

        private GameObject Create(Action<GameObject> Expression)
        {
            GameObject newObj = new();

            Expression(newObj);

            return newObj;
        }
    }
}
