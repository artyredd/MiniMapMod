using System;
using BepInEx;
using BepInEx.Configuration;
using MiniMapLibrary;
using MiniMapMod.Adapters;
using RoR2;
using RoR2.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MiniMapMod
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [DefaultExecutionOrder(1000)]
    public class MiniMapPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "MiniMap";
        public const string PluginName = "Mini Map Mod";
        public const string PluginVersion = "4.2.0";

        private readonly SceneBounds bounds = new();
        private MiniMapLibrary.ILogger logger;
        private SpriteManager sprites;
        private Minimap minimap;
        private SceneTracker tracker;
        private MarkerRenderer renderer;
        private Run currentRun;
        private HUD hud;
        private CameraRigController cameraRig;
        private SceneInfo sceneInfo;
        private GameObject viewedObject;
        private Transform viewedTransform;
        private Vector3 viewerPosition;
        private float zoom = 1f;
        private float nextHudAttempt, nextInteractionScan, nextBodyScan, nextStateRefresh, nextSceneScan, nextErrorLog, nextDiagnostics;
        private float nextLayoutCheck;
        private float lastScale;
        private string sceneChangeReason = "enable";
        private bool initialized, subscribed, sceneDirty;
        private volatile bool configurationDirty, zoomDirty;
        private bool visible = true;
        private int spriteRevision = -1;
        private int sceneScansThisStage;

        private void Awake()
        {
            if (Application.isBatchMode)
                return;

            logger = new Log(Logger);
            try
            {
                var config = new ConfigAdapter(Config);
                Settings.LoadApplicationSettings(logger, config);
                Settings.LoadConfigEntries(config);

                zoom = Settings.MinimapScale;
                sprites = new SpriteManager(logger, asynchronous: true);
                minimap = new Minimap(logger);
                tracker = new SceneTracker(minimap, sprites, logger, () => viewerPosition.y);
                renderer = new MarkerRenderer(minimap, sprites, tracker);
                initialized = true;
                logger.LogInfo($"MiniMap {PluginVersion} initialized.");
                logger.LogDebug($"[scanner.init] interactables={Settings.InteractionScanInterval:F2}s bodies={Settings.BodyScanInterval:F2}s sceneFallback={Settings.SceneScanInterval:F2}s state={Settings.StateRefreshInterval:F2}s; worldRadius={Settings.WorldRadius} (0=automatic) zoom={zoom:F2}.");
            }
            catch (Exception exception)
            {
                logger.LogException(exception, "MiniMap could not initialize.");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (!initialized || subscribed)
                return;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            Stage.onStageStartGlobal += OnStageStarted;
            Run.onRunDestroyGlobal += OnRunDestroyed;
            Config.SettingChanged += OnConfigurationChanged;
            subscribed = true;
            sceneDirty = true;
        }

        private void OnDisable()
        {
            if (subscribed)
            {
                SceneManager.activeSceneChanged -= OnActiveSceneChanged;
                Stage.onStageStartGlobal -= OnStageStarted;
                Run.onRunDestroyGlobal -= OnRunDestroyed;
                Config.SettingChanged -= OnConfigurationChanged;
                subscribed = false;
            }
            ResetScene();
        }

        private void OnDestroy()
        {
            OnDisable();
            sprites?.Dispose();
            sprites = null;
            initialized = false;
        }

        // Only set flags here; an exception thrown inside a global event would hit other subscribers.
        private void OnActiveSceneChanged(Scene previous, Scene current) { sceneDirty = true; sceneChangeReason = $"scene {previous.name} -> {current.name}"; }
        private void OnStageStarted(Stage stage) { sceneDirty = true; sceneChangeReason = "stage start"; }
        private void OnRunDestroyed(Run run) { sceneDirty = true; sceneChangeReason = "run destroyed"; }

        private void OnConfigurationChanged(object sender, SettingChangedEventArgs args)
        {
            ConfigDefinition definition = args.ChangedSetting.Definition;
            if (definition.Section == "Settings.Zoom" && definition.Key == "zoomLevel")
                zoomDirty = true;
            else
                configurationDirty = true;
        }

        private void Update()
        {
            if (!initialized)
                return;
            float now = Time.unscaledTime;
            try
            {
                if (sceneDirty || sceneInfo != SceneInfo.instance || currentRun != Run.instance)
                {
                    ResetScene();
                    sceneInfo = SceneInfo.instance;
                    currentRun = Run.instance;
                    sceneDirty = false;
                    logger.LogDebug($"[scene] {sceneChangeReason}; active={SceneManager.GetActiveScene().name} run={(bool)Run.instance} sceneInfo={(bool)sceneInfo}. Tracking and UI cleared.");
                }
                if (configurationDirty)
                {
                    Settings.Refresh();
                    tracker.Clear();
                    renderer.Reset();
                    minimap.Destroy();
                    hud = null;
                    nextHudAttempt = nextInteractionScan = nextBodyScan = nextStateRefresh = nextSceneScan = 0f;
                    sceneScansThisStage = 0;
                    configurationDirty = false;
                    logger.LogDebug("[config] Refreshed existing settings and rebuilt marker state.");
                }
                if (zoomDirty)
                {
                    zoom = Settings.MinimapScale;
                    zoomDirty = false;
                }
                if (!Run.instance)
                    return;

                HandleInput();
                if (!visible)
                    return;

                // Keep scanning even without a HUD so diagnostics can tell the two failures apart.
                if (now >= nextHudAttempt)
                    EnsureHud(now);
                if (hud && cameraRig && cameraRig.sceneCam)
                    UpdateViewer();
                if (now >= nextSceneScan)
                {
                    tracker.RefreshSceneCandidates();
                    sceneScansThisStage++;
                    // Vanilla objects all register with the game, so the full search is only a
                    // safety net: a few passes while the stage populates, then only if it ever
                    // turned up something unregistered (typically from another mod).
                    nextSceneScan = sceneScansThisStage < 3 || tracker.UnregisteredCandidates > 0
                        ? now + Settings.SceneScanInterval : float.PositiveInfinity;
                    nextInteractionScan = 0f;
                }
                if (now >= nextInteractionScan)
                {
                    nextInteractionScan = now + Settings.InteractionScanInterval;
                    tracker.RefreshInteractables();
                    bounds.Refresh(sceneInfo, tracker, viewerPosition);
                }
                if (now >= nextBodyScan)
                {
                    nextBodyScan = now + Settings.BodyScanInterval;
                    tracker.RefreshBodies(viewedObject);
                    tracker.RefreshPickups();
                }
                if (now >= nextStateRefresh)
                {
                    nextStateRefresh = now + Settings.StateRefreshInterval;
                    tracker.UpdateStates();
                }
            }
            catch (Exception exception)
            {
                HandleFailure(exception, now);
            }
            finally
            {
                if (Settings.DebugLogging && now >= nextDiagnostics)
                {
                    nextDiagnostics = now + 5f;
                    ReportDiagnostics();
                }
            }
        }

        private void HandleInput()
        {
            if (ChatBox.IsActive)
                return;
            if (Input.GetKeyDown(Settings.MinimapKey))
            {
                visible = !visible;
                minimap.SetVisible(visible);
                logger.LogDebug($"[visibility] visible={visible} key={Settings.MinimapKey}.");
                if (visible)
                    nextHudAttempt = nextInteractionScan = nextBodyScan = nextStateRefresh = 0f;
            }
            if (!visible)
                return;

            float nextZoom = zoom;
            if (Input.GetKeyDown(Settings.MinimapIncreaseScaleKey))
                nextZoom *= 1.1f;
            else if (Input.GetKeyDown(Settings.MinimapDecreaseScaleKey))
                nextZoom /= 1.1f;
            nextZoom = Numeric.Clamp(nextZoom, 0.1f, 20f, 1f);
            if (Math.Abs(nextZoom - zoom) > 0.00001f)
            {
                zoom = nextZoom;
                Settings.SetMinimapScale(zoom);
            }
        }

        private bool EnsureHud(float now)
        {
            if (hud && hud.isActiveAndEnabled && cameraRig && cameraRig == hud.cameraRigController && cameraRig.sceneCam && minimap.Created)
            {
                if (now >= nextLayoutCheck)
                {
                    nextLayoutCheck = now + 0.5f;
                    GameObject nextParent = FindMapParent(out bool useLayout);
                    if (nextParent && minimap.SetHudParent(nextParent, useLayout))
                        logger.LogDebug($"[hud.layout] Adopted late run-info layout: parent={nextParent.name} objectiveLayout={useLayout}.");
                }
                return true;
            }

            tracker.DetachIcons();
            minimap.Destroy();
            hud = null;
            cameraRig = null;
            nextHudAttempt = now + 0.5f;
            var huds = HUD.readOnlyInstanceList;
            for (int i = 0; i < huds.Count; i++)
            {
                HUD candidate = huds[i];
                if (!candidate || !candidate.isActiveAndEnabled || candidate.localUserViewer == null
                    || !candidate.cameraRigController || !candidate.cameraRigController.sceneCam)
                    continue;

                hud = candidate;
                cameraRig = candidate.cameraRigController;
                break;
            }
            if (!hud)
                return false;

            GameObject parent = FindMapParent(out bool useObjectiveLayout);
            if (!parent || !parent.GetComponentInParent<Canvas>())
                return false;
            sprites.PreloadConfiguredIcons();
            minimap.CreateMinimap(sprites, parent, useObjectiveLayout);
            spriteRevision = -1;
            nextHudAttempt = 0f;
            nextLayoutCheck = now + 0.5f;
            logger.LogDebug($"[hud] parent={parent.name} objectiveLayout={useObjectiveLayout} {minimap.DescribeUI()}");
            return minimap.Created;
        }

        private GameObject FindMapParent(out bool useObjectiveLayout)
        {
            ObjectivePanelController objectives = hud.GetComponentInChildren<ObjectivePanelController>(true);
            if (objectives && objectives.transform.IsChildOf(hud.transform))
            {
                // RightInfoBar (the objective panel's parent) is the column that lays everything out.
                Transform column = objectives.transform.parent;
                if (column && column.GetComponent<VerticalLayoutGroup>() is VerticalLayoutGroup layout && layout.enabled)
                {
                    useObjectiveLayout = true;
                    return column.gameObject;
                }
                if (objectives.GetComponent<VerticalLayoutGroup>() is VerticalLayoutGroup innerLayout && innerLayout.enabled)
                {
                    useObjectiveLayout = true;
                    return objectives.gameObject;
                }
            }
            useObjectiveLayout = false;
            return hud.mainContainer;
        }

        private void UpdateViewer()
        {
            GameObject target = cameraRig.target ? cameraRig.target : hud.targetBodyObject;
            if (target != viewedObject)
            {
                viewedObject = target;
                viewedTransform = target ? target.transform : null;
                tracker.ViewerInteractor = target ? target.GetComponent<Interactor>() : null;
                nextBodyScan = nextStateRefresh = 0f;
            }
            viewerPosition = viewedTransform ? viewedTransform.position : cameraRig.sceneCam.transform.position;
            minimap.SetPlayerVisible(viewedTransform && Settings.GetSetting(InteractableKind.Player).Enabled);
        }

        private void LateUpdate()
        {
            if (!initialized)
                return;
            renderer.BeginFrame();
            renderer.Diagnostics = Settings.DebugLogging;
            if (!Run.instance || !visible || !minimap.Created || !cameraRig || !cameraRig.sceneCam || !hud)
                return;
            try
            {
                UpdateViewer();
                minimap.SetViewRotation(cameraRig.sceneCam.transform.rotation);
                if (spriteRevision != sprites.Revision)
                {
                    minimap.RefreshPlayerSprite(sprites);
                    spriteRevision = sprites.Revision;
                }
                lastScale = MapProjection.GetScale(bounds.Range, Settings.MinimapSize, zoom, Settings.WorldRadius);
                renderer.Render(tracker.Bodies, viewerPosition, lastScale);
                renderer.Render(tracker.Interactables, viewerPosition, lastScale);
                renderer.Render(tracker.Pickups, viewerPosition, lastScale);
            }
            catch (Exception exception)
            {
                HandleFailure(exception, Time.unscaledTime);
            }
        }

        private void ReportDiagnostics()
        {
            logger.LogDebug($"[state] scene={SceneManager.GetActiveScene().name} run={(bool)Run.instance} sceneInfo={(bool)SceneInfo.instance} stage={(bool)Stage.instance} visible={visible} hud={(bool)hud} camera={(bool)cameraRig} viewer={(bool)viewedTransform} center={viewerPosition} radius={Settings.WorldRadius} zoom={zoom:F2} scale={lastScale:F5} bounds={bounds.Source}.");
            logger.LogDebug($"[scan.scene] calls={tracker.SceneScans} components={tracker.SceneComponents} unregistered={tracker.UnregisteredCandidates} milliseconds={tracker.SceneScanMilliseconds:F2} next={(float.IsPositiveInfinity(nextSceneScan) ? "never" : (nextSceneScan - Time.unscaledTime).ToString("F1") + "s")}; [scan.interactables] {tracker.InteractionScan}; [scan.bodies] {tracker.BodyScan}; [scan.pickups] {tracker.PickupScan}.");
            logger.LogDebug($"[track] interactables={tracker.Interactables.Count} bodies={tracker.Bodies.Count} pickups={tracker.Pickups.Count} activeStates={tracker.ActiveStates} scannerHidden={tracker.HiddenStates} repeatedErrors={tracker.RepeatedErrors}; [render] {renderer}.");
            logger.LogDebug($"[ui] {minimap.DescribeUI()}; [sprites] {sprites.DescribeRequests()}");
        }

        private void ResetScene()
        {
            tracker?.Clear();
            renderer?.Reset();
            minimap?.Destroy();
            bounds.Clear();
            hud = null;
            cameraRig = null;
            viewedObject = null;
            viewedTransform = null;
            sceneInfo = null;
            currentRun = null;
            viewerPosition = Vector3.zero;
            lastScale = 0f;
            nextHudAttempt = nextInteractionScan = nextBodyScan = nextStateRefresh = nextSceneScan = 0f;
            sceneScansThisStage = 0;
        }

        private void HandleFailure(Exception exception, float now)
        {
            if (now >= nextErrorLog)
            {
                nextErrorLog = now + 10f;
                logger.LogException(exception, "MiniMap update failed; retrying after a short delay.");
            }
            ResetScene();
            sceneInfo = SceneInfo.instance;
            currentRun = Run.instance;
            nextHudAttempt = now + 2f;
        }
    }
}
