using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMapLibrary
{
    public partial class Minimap
    {
        private const int MaximumPooledIcons = 256;
        private readonly ILogger logger;
        private readonly Dictionary<InteractableKind, Stack<RectTransform>> iconPool = new();
        private RectTransform containerTransform;
        private RectTransform bodyLayer, interactableLayer, pickupLayer;
        private Image playerImage;
        private Transform poolRoot;
        private int pooledCount;
        private float yaw = float.NaN;
        private bool rotatesContainer;
        private RectTransform viewportTransform;
        private Image backgroundImage;
        private bool usesObjectiveLayout;
        public int IconsCreated { get; private set; }
        public int IconsReused { get; private set; }
        public int IconFailures { get; private set; }
        public bool PlayerVisible => playerImage && playerImage.gameObject.activeInHierarchy && playerImage.color.a > 0f && playerImage.sprite;

        public static GameObject gameObject { get; private set; }
        public static GameObject Container { get; private set; }
        public bool Created => gameObject && Container && containerTransform;
        public int RotationVersion { get; private set; }
        public float RotationSine { get; private set; }
        public float RotationCosine { get; private set; } = 1f;
        public Quaternion IconRotation { get; private set; } = Quaternion.identity;

        public Minimap(ILogger logger) => this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public void CreateMinimap(ISpriteManager spriteManager, GameObject Parent) => CreateMinimap(spriteManager, Parent, true);

        public void CreateMinimap(ISpriteManager spriteManager, GameObject parent, bool useObjectiveLayout)
        {
            if (Created)
                return;
            if (!parent)
                throw new ArgumentNullException(nameof(parent));

            // Live inside the HUD's own canvas; a separate canvas on the Default layer gets culled by the UI camera.
            gameObject = new GameObject("MiniMap", typeof(RectTransform), typeof(LayoutElement));
            var slot = (RectTransform)gameObject.transform;
            SetHudParent(parent, useObjectiveLayout);

            var panel = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            var panelTransform = (RectTransform)panel.transform;
            viewportTransform = panelTransform;
            Helpers.Transforms.SetParent(panelTransform, slot);
            panelTransform.anchorMin = panelTransform.anchorMax = panelTransform.pivot = Vector2.one;
            panelTransform.sizeDelta = new Vector2(Settings.ViewfinderSize.Width, Settings.ViewfinderSize.Height);
            panelTransform.anchoredPosition = Settings.PositionOffset;
            Image background = panel.GetComponent<Image>();
            backgroundImage = background;
            background.color = Settings.BackgroundColor;
            background.raycastTarget = false;

            Container = new GameObject("Icons", typeof(RectTransform));
            containerTransform = (RectTransform)Container.transform;
            Helpers.Transforms.SetParent(containerTransform, panelTransform);
            containerTransform.anchorMin = containerTransform.anchorMax = new Vector2(0.5f, 0.5f);
            containerTransform.anchoredPosition = Vector2.zero;
            containerTransform.sizeDelta = panelTransform.sizeDelta;
            bodyLayer = CreateLayer("Characters");
            interactableLayer = CreateLayer("Interactables");
            // Drops usually land next to the chest they came from; keep them on top.
            pickupLayer = CreateLayer("Pickups");

            var inactivePool = new GameObject("IconPool", typeof(RectTransform));
            poolRoot = inactivePool.transform;
            Helpers.Transforms.SetParent(poolRoot, slot);
            inactivePool.SetActive(false);

            InteractibleSetting playerSetting = Settings.GetSetting(InteractableKind.Player);
            if (playerSetting.Enabled)
            {
                GameObject player = Helpers.Sprites.CreateIcon(spriteManager.GetSprite(InteractableKind.Player),
                    playerSetting.Dimensions.Width, playerSetting.Dimensions.Height, playerSetting.ActiveColor);
                player.name = "ViewedCharacter";
                Helpers.Transforms.SetParent(player.transform, panelTransform);
                Center((RectTransform)player.transform);
                playerImage = player.GetComponent<Image>();
            }
            yaw = float.NaN;
            SetRotation(Quaternion.identity);
            logger.LogDebug($"Minimap HUD created: layer={gameObject.layer}, size={panelTransform.sizeDelta}, backgroundAlpha={background.color.a:F2}, playerSprite={(playerImage && playerImage.sprite)}, inheritedCanvas={(gameObject.GetComponentInParent<Canvas>() != null)}.");
        }

        // The run-info panel shows up after the main HUD, so the map moves there once it exists.
        public bool SetHudParent(GameObject parent, bool useObjectiveLayout)
        {
            if (!gameObject || !parent)
                return false;
            var slot = (RectTransform)gameObject.transform;
            if (slot.parent == parent.transform && usesObjectiveLayout == useObjectiveLayout)
                return false;

            Helpers.Transforms.SetParent(slot, parent.transform);
            usesObjectiveLayout = useObjectiveLayout;
            slot.anchorMin = slot.anchorMax = slot.pivot = Vector2.one;
            // Reserve room for a negative Y offset too, or the map overlaps the objectives.
            slot.sizeDelta = new Vector2(Settings.ViewfinderSize.Width,
                Settings.ViewfinderSize.Height + Math.Max(16f, -Settings.PositionOffset.y + 8f));
            LayoutElement layout = gameObject.GetComponent<LayoutElement>();
            layout.ignoreLayout = !useObjectiveLayout;
            layout.minHeight = layout.preferredHeight = slot.sizeDelta.y;
            layout.preferredWidth = Settings.ViewfinderSize.Width;
            layout.flexibleWidth = layout.flexibleHeight = 0f;
            slot.anchoredPosition = useObjectiveLayout ? Vector2.zero : new Vector2(-24f, -160f);
            if (useObjectiveLayout)
            {
                slot.SetAsFirstSibling();
                if (parent.transform is RectTransform rect)
                    LayoutRebuilder.MarkLayoutForRebuild(rect);
            }
            return true;
        }

        private RectTransform CreateLayer(string name)
        {
            var layer = new GameObject(name, typeof(RectTransform));
            var transform = (RectTransform)layer.transform;
            Helpers.Transforms.SetParent(transform, containerTransform);
            Center(transform);
            transform.sizeDelta = containerTransform.sizeDelta;
            return transform;
        }

        private static void Center(RectTransform transform)
        {
            transform.anchorMin = transform.anchorMax = transform.pivot = new Vector2(0.5f, 0.5f);
            transform.anchoredPosition = Vector2.zero;
        }

        public void RefreshPlayerSprite(ISpriteManager sprites)
        {
            if (playerImage)
                playerImage.sprite = sprites.GetSprite(InteractableKind.Player);
        }

        public void SetPlayerVisible(bool visible)
        {
            if (playerImage && playerImage.gameObject.activeSelf != visible)
                playerImage.gameObject.SetActive(visible);
        }

        public string DescribeUI()
        {
            if (!Created)
                return "created=false";
            Canvas canvas = gameObject.GetComponentInParent<Canvas>();
            Canvas rootCanvas = canvas ? canvas.rootCanvas : null;
            Camera camera = rootCanvas ? rootCanvas.worldCamera : null;
            float parentAlpha = 1f;
            foreach (CanvasGroup group in gameObject.GetComponentsInParent<CanvasGroup>(true))
            {
                parentAlpha *= group.alpha;
                if (group.ignoreParentGroups) break;
            }
            return $"created=true hierarchyActive={gameObject.activeInHierarchy} layer={gameObject.layer}"
                + $" parent={gameObject.transform.parent.name} objectiveLayout={usesObjectiveLayout}"
                + $" canvas={(canvas ? canvas.name : "missing")} canvasEnabled={(canvas && canvas.isActiveAndEnabled)}"
                + $" rootCanvas={(rootCanvas ? rootCanvas.name : "missing")} renderMode={(rootCanvas ? rootCanvas.renderMode.ToString() : "none")}"
                + $" camera={(camera ? camera.name : "none")} mask={(camera ? camera.cullingMask.ToString("X8") : "none")}"
                + $" layerInMask={(!camera || (camera.cullingMask & (1 << gameObject.layer)) != 0)}"
                + $" rect={viewportTransform.rect.size} scale={gameObject.transform.lossyScale}"
                + $" backgroundAlpha={backgroundImage.color.a:F2} parentAlpha={parentAlpha:F2} backgroundCulled={backgroundImage.canvasRenderer.cull}"
                + $" player={PlayerVisible} createdIcons={IconsCreated} reusedIcons={IconsReused} pooledIcons={pooledCount} iconFailures={IconFailures}";
        }

        public void SetVisible(bool visible)
        {
            if (gameObject && gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        public void SetRotation(Quaternion rotation) => UpdateRotation(rotation, true);

        // Rotation is applied to projected positions instead, so icons don't need counter-rotating.
        public void SetViewRotation(Quaternion rotation) => UpdateRotation(rotation, false);

        private void UpdateRotation(Quaternion rotation, bool rotateContainer)
        {
            if (!containerTransform)
                return;
            float nextYaw = Settings.RotateWithCamera ? rotation.eulerAngles.y : 0f;
            if (!Numeric.IsFinite(nextYaw))
                nextYaw = 0f;
            if (rotatesContainer == rotateContainer && Numeric.IsFinite(yaw) && Mathf.Abs(Mathf.DeltaAngle(nextYaw, yaw)) < 0.01f)
                return;
            yaw = nextYaw;
            if (rotateContainer || rotatesContainer)
                containerTransform.localRotation = rotateContainer ? Quaternion.Euler(0f, 0f, yaw) : Quaternion.identity;
            rotatesContainer = rotateContainer;
            IconRotation = rotateContainer ? Quaternion.Euler(0f, 0f, -yaw) : Quaternion.identity;
            float radians = yaw * Mathf.Deg2Rad;
            RotationSine = Mathf.Sin(radians);
            RotationCosine = Mathf.Cos(radians);
            RotationVersion++;
        }

        public RectTransform CreateIcon(InteractableKind type, Vector3 minimapPosition, ISpriteManager spriteManager)
        {
            try { return CreateIconCore(type, minimapPosition, spriteManager); }
            catch
            {
                IconFailures++;
                throw;
            }
        }

        private RectTransform CreateIconCore(InteractableKind type, Vector3 minimapPosition, ISpriteManager spriteManager)
        {
            InteractableKind kind = type;
            Vector3 position = minimapPosition;
            if (!Created)
            {
                IconFailures++;
                return null;
            }
            RectTransform icon = null;
            if (iconPool.TryGetValue(kind, out Stack<RectTransform> pool))
            {
                while (pool.Count > 0 && !icon)
                {
                    icon = pool.Pop();
                    pooledCount--;
                }
            }

            if (!icon)
            {
                InteractibleSetting setting = Settings.GetSetting(kind);
                GameObject newIcon = Helpers.Sprites.CreateIcon(spriteManager.GetSprite(kind),
                    setting.Dimensions.Width, setting.Dimensions.Height, setting.ActiveColor);
                newIcon.name = kind.ToString();
                icon = (RectTransform)newIcon.transform;
                IconsCreated++;
            }
            else
                IconsReused++;

            const InteractableKind characters = InteractableKind.EnemyMonster | InteractableKind.EnemyLunar
                | InteractableKind.EnemyVoid | InteractableKind.EnemyEndOfCycle | InteractableKind.Player
                | InteractableKind.Minion | InteractableKind.Neutral;
            Helpers.Transforms.SetParent(icon, kind == InteractableKind.Pickup ? pickupLayer
                : (kind & characters) != 0 ? bodyLayer : interactableLayer);
            Center(icon);
            icon.localPosition = position;
            icon.localRotation = IconRotation;
            icon.gameObject.SetActive(true);
            return icon;
        }

        public void ReleaseIcon(InteractableKind kind, RectTransform icon)
        {
            if (!icon)
                return;
            icon.gameObject.SetActive(false);
            if (!Created || !poolRoot || pooledCount >= MaximumPooledIcons)
            {
                UnityEngine.Object.Destroy(icon.gameObject);
                return;
            }

            icon.SetParent(poolRoot, false);
            if (!iconPool.TryGetValue(kind, out Stack<RectTransform> pool))
            {
                pool = new Stack<RectTransform>();
                iconPool.Add(kind, pool);
            }
            pool.Push(icon);
            pooledCount++;
        }

        public void DestroyIcons()
        {
            if (bodyLayer)
                DestroyChildren(bodyLayer);
            if (interactableLayer)
                DestroyChildren(interactableLayer);
            if (pickupLayer)
                DestroyChildren(pickupLayer);
            if (poolRoot)
                DestroyChildren(poolRoot);
            iconPool.Clear();
            pooledCount = 0;
        }

        private static void DestroyChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }

        public void Destroy()
        {
            if (gameObject)
            {
                gameObject.SetActive(false);
                UnityEngine.Object.Destroy(gameObject);
            }
            gameObject = null;
            Container = null;
            containerTransform = null;
            bodyLayer = interactableLayer = pickupLayer = null;
            playerImage = null;
            poolRoot = null;
            iconPool.Clear();
            pooledCount = 0;
            yaw = float.NaN;
            rotatesContainer = false;
            viewportTransform = null;
            backgroundImage = null;
            IconsCreated = IconsReused = IconFailures = 0;
        }
    }
}
