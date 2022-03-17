using BepInEx;
using RoR2;
using UnityEngine;
using MiniMapLibrary;
using System.Collections.Generic;
using System.Linq;
using System;
using BepInEx.Configuration;
using MiniMapMod.Adapters;
using MiniMapLibrary.Config;
using MiniMapLibrary.Scanner;

namespace MiniMapMod
{
    [BepInPlugin("MiniMap", "Mini Map Mod", "3.1.6")]
    public class MiniMapPlugin : BaseUnityPlugin
    {
        private readonly ISpriteManager SpriteManager = new SpriteManager();

        private readonly List<ITrackedObject> TrackedObjects = new();

        private Minimap Minimap;

        private readonly Range3D TrackedDimensions = new();

        private bool Enable = true;

        private bool ScannedStaticObjects = false;

        private IConfig config;

        private MiniMapLibrary.ILogger logger;

        public void Awake()
        {
            // wrap the bepinex logger with an adapter so 
            // we can pass it to the business layer
            logger = new Log(base.Logger);

            // create the minimap controller
            Minimap = new(logger);

            // SETUP CONFIG

            // wrap bepinex config so we can pass it to business layer
            config = new ConfigAdapter(this.Config);

            Settings.LoadApplicationSettings(config);

            logger.LogInfo($"Loaded log level: {Settings.LogLevel}");

            // bind options
            InteractableKind[] kinds = Enum.GetValues(typeof(InteractableKind)).Cast<InteractableKind>().Where(x => x != InteractableKind.none && x != InteractableKind.All).ToArray();

            foreach (var item in kinds)
            {
                Settings.LoadConfigEntries(item, config);

                InteractibleSetting setting = Settings.GetSetting(item);
                logger.LogInfo($"Loaded {item} config [{(setting.Config.Enabled.Value ? "enabled" : "disabled")}, {setting.ActiveColor}, {setting.InactiveColor}, ({setting.Dimensions.Width}x{setting.Dimensions.Height})]");
            }

            logger.LogInfo("Creating scene scan hooks");

            // hook events so the minimaps updates

            // scan scene should NEVER throw exceptions
            // doing so prevents all other subscribing events to not fire (after the exception)

            // this will re-scan the scene every time any npc, player dies
            GlobalEventManager.onCharacterDeathGlobal += (x) => ScanScene();

            // this will re-scan when the player uses anything like a chest
            // or the landing pod
            GlobalEventManager.OnInteractionsGlobal += (x, y, z) => ScanScene();

            // resposible for scanning chests
            IScanner<ChestBehavior> chestScanner = new MonoBehaviorScanner<ChestBehavior>(logger);

            // responsible for finding the InteractibleKind for each chest
            IInteractibleSorter<ChestBehavior> chestSorter = new MonoBehaviourSorter<ChestBehavior>(
                new ISorter<ChestBehavior>[] {
                    new DefaultSorter<ChestBehavior>(InteractableKind.Chest, (x) => x.gameObject, (x) => true),
                    new DefaultSorter<ChestBehavior>(InteractableKind.LunarPod, (x) => x.gameObject, (x) => true),
                }
            );

            ITrackedObjectScanner sceneScanner = new TrackedObjectScanner<ChestBehavior>(false, chestScanner, chestSorter);

            ssceneScanner.ScanScene();
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.M))
            {
                Enable = !Enable;

                if (Enable == false)
                {
                    logger.LogInfo("Resetting minimap");
                    Reset();
                    return;
                }
            }

            if (Enable)
            {
                // the main camera becomes null when the scene ends on death or quits
                if (Camera.main == null)
                {
                    logger.LogDebug("Main camera was null, resetting minimap");
                    Reset();
                    return;
                }

                if (Minimap.Created)
                {
                    try
                    {
                        Minimap.SetRotation(Camera.main.transform.rotation);

                        UpdateIconPositions();
                    }
                    catch (NullReferenceException)
                    {
                        logger.LogDebug($"{nameof(NullReferenceException)} was encountered while updating positions, reseting minimap");
                        Reset();
                    }
                }
                else
                {
                    if (TryCreateMinimap())
                    {
                        TrackedDimensions.Clear();

                        ScanScene();
                    }
                }
            }
        }

        private void UpdateIconPositions()
        {
            // only perform this calculation once per frame
            Vector2 cameraPositionMinimap = Camera.main.transform.position.ToMinimapPosition(TrackedDimensions);

            for (int i = 0; i < TrackedObjects.Count; i++)
            {
                ITrackedObject item = TrackedObjects[i];

                // if we dont have a reference to the gameobject any more, remove it and continue
                if (item.gameObject == null)
                {
                    TrackedObjects.RemoveAt(i);

                    // if we still have a icon for the now discarded item destroy it
                    if (item.MinimapTransform != null)
                    {
                        GameObject.Destroy(item.MinimapTransform.gameObject);
                    }

                    continue;
                }

                // convert the world positions to minimap positions
                // remember the minimap is calculated on a scale from 0d to 1d where 0d is the least most coord of any interactible and 1d is the largest coord of any interactible

                Vector2 itemMinimapPosition = item.gameObject.transform.position.ToMinimapPosition(TrackedDimensions) - cameraPositionMinimap;

                // there exists no icon when .MinimapTransform is null
                if (item.MinimapTransform == null)
                {
                    // create one
                    item.MinimapTransform = Minimap.CreateIcon(item.InteractableType, itemMinimapPosition, this.SpriteManager);
                }
                else
                {
                    // since it was already created update the position
                    item.MinimapTransform.localPosition = itemMinimapPosition;

                    // becuase we don't want the icons to spin WITH the minimap set their rotation
                    // every frame so they're always facing right-side up
                    // (they inherit their position from the minimap)
                    item.MinimapTransform.rotation = Quaternion.identity;
                }

                // check to see if its active and whether to change its color
                item.CheckActive();
            }
        }

        private bool TryCreateMinimap()
        {
            GameObject objectivePanel = GameObject.Find("ObjectivePanel");

            if (objectivePanel == null || this.SpriteManager == null)
            {
                Minimap.Destroy();
                return false;
            }

            logger.LogInfo("MINIMAP: Creating Minimap object");

            Minimap.CreateMinimap(this.SpriteManager, objectivePanel.gameObject);

            logger.LogInfo("MINIMAP: Finished creating Minimap");

            return true;
        }

        private void Reset()
        {
            logger.LogDebug($"Clearing {nameof(TrackedObjects)}");
            TrackedObjects.Clear();

            logger.LogDebug($"Clearing {nameof(TrackedDimensions)}");
            TrackedDimensions.Clear();

            logger.LogDebug($"Destroying {nameof(Minimap)}");
            Minimap.Destroy();

            // mark the scene as scannable again so we scan for chests etc..
            ScannedStaticObjects = false;
        }

        private void ScanScene()
        {
            // when other mods hook into the various global events
            // and this method throws exceptions, the entire event will throw and fail to invoke their methods
            // as a result, this method should never throw an exception and should output meaningful
            // errors
            try
            {
                logger.LogDebug("Scanning Scene");

                logger.LogDebug("Clearing dynamically tracked objects");
                ClearDynamicTrackedObjects();

                logger.LogDebug("Scanning static types");
                ScanStaticTypes();

                logger.LogDebug("Scanning dynamic types");
                ScanDynamicTypes();
            }
            catch (Exception e)
            {
                logger.LogException(e, $"Fatal exception within minimap");

                // intentionally consume the error, again we never want to throw an exception in
                // a global event delegate (unless we're the last event, but that is never garunteed)
            }
        }

        private void ScanStaticTypes()
        {
            // if we have alreadys scanned don't scan again until we die or the scene changes (this method has sever performance implications)
            if (ScannedStaticObjects)
            {
                return;
            }

            // NON lunar pods
            RegisterMonobehaviorType<ChestBehavior>(InteractableKind.Chest, dynamicObject: false,
                selector: chest => {
                    var token = chest?.GetComponent<PurchaseInteraction>()?.contextToken;

                    // mods that implement ChestBehaviour, may not also PurchaseInteraction
                    if (token is null)
                    {
                        logger.LogDebug($"No {nameof(PurchaseInteraction)} component on {nameof(ChestBehavior)}. GameObject.name = {chest.gameObject.name}");
                        return false;
                    }

                    return token.Contains("CHEST") && token != "LUNAR_CHEST_CONTEXT" && token.Contains("STEALTH") == false;
                });

            // lunar pods
            RegisterMonobehaviorType<ChestBehavior>(InteractableKind.LunarPod, dynamicObject: false,
                selector: chest => {
                    var token = chest?.GetComponent<PurchaseInteraction>()?.contextToken;

                    // mods that implement ChestBehaviour, may not also PurchaseInteraction
                    if (token is null)
                    {
                        logger.LogDebug($"No {nameof(PurchaseInteraction)} component on {nameof(ChestBehavior)}. GameObject.name = {chest.gameObject.name}");

                        // since we're explicitly looking for lunar pods here DONT return true
                        return false;
                    }

                    return token == "LUNAR_CHEST_CONTEXT";
                });

            // fans
            RegisterMonobehaviorType<PurchaseInteraction>(InteractableKind.Special, dynamicObject: false,
                selector: chest => chest?.contextToken == "FAN_CONTEXT");

            // adapative chests
            RegisterMonobehaviorType<RouletteChestController>(InteractableKind.Chest, dynamicObject: false);

            // shrines
            RegisterMonobehaviorType<ShrineBloodBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineChanceBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineBossBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineCombatBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineHealingBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineRestackBehavior>(InteractableKind.Shrine, dynamicObject: false);

            // normal shops
            RegisterMonobehaviorType<ShopTerminalBehavior>(InteractableKind.Chest, dynamicObject: false,
                selector: shop => {

                    var token = shop?.GetComponent<PurchaseInteraction>()?.contextToken;

                    // mods that implement ShopTerminalBehavior, may not also PurchaseInteraction
                    if (token is null)
                    {
                        logger.LogDebug($"No {nameof(PurchaseInteraction)} component on {nameof(ShopTerminalBehavior)}. GameObject.name = {shop?.gameObject?.name}");

                        // since we're explicitly looking for lunar pods here DONT return true
                        return false;
                    }

                    return token != "DUPLICATOR_CONTEXT";
                });

            // duplicators
            RegisterMonobehaviorType<PurchaseInteraction>(InteractableKind.Printer, dynamicObject: false,
                selector: shop => shop?.contextToken?.Contains("DUPLICATOR") ?? false);

            // barrels
            RegisterMonobehaviorType<BarrelInteraction>(InteractableKind.Barrel, barrel => !barrel.Networkopened, dynamicObject: false);

            // scrapper
            RegisterMonobehaviorType<ScrapperController>(InteractableKind.Utility, dynamicObject: false);

            // random stuff like the exploding backpack door on the landing pod
            RegisterMonobehaviorType<GenericInteraction>(InteractableKind.Special, dynamicObject: false);

            // boss teleporter
            RegisterMonobehaviorType<TeleporterInteraction>(InteractableKind.Teleporter, (teleporter) => teleporter.activationState != TeleporterInteraction.ActivationState.Charged, dynamicObject: false);

            // drones
            RegisterMonobehaviorType<SummonMasterBehavior>(InteractableKind.Drone, dynamicObject: false);

            // make sure we only do this once per scene
            ScannedStaticObjects = true;
        }

        private void ScanDynamicTypes()
        {
            logger.LogDebug($"Scanning {nameof(AimAssistTarget)} types (enemies)");
            RegisterMonobehaviorType<AimAssistTarget>(InteractableKind.Enemy, x => true, dynamicObject: true);
        }

        private void ClearDynamicTrackedObjects()
        {
            if (ScannedStaticObjects is false)
            {
                return;
            }

            for (int i = 0; i < TrackedObjects.Count; i++)
            {
                var obj = TrackedObjects[i];

                if (obj.DynamicObject)
                {
                    obj.Destroy();
                    TrackedObjects.RemoveAt(i);
                }
            }
        }

        private void RegisterMonobehaviorType<T>(InteractableKind kind, Func<T, bool> ActiveChecker = null, bool dynamicObject = true, Func<T, bool> selector = null) where T : MonoBehaviour
        {

            logger.LogDebug($"Scanning {typeof(T).Name} types for {kind}s");

            bool enabled = Settings.GetSetting(kind)?.Config?.Enabled?.Value ?? false;

            logger.LogDebug($"{kind} enabled: {enabled}");

            if (enabled == false)
            {
                return;
            }

            selector ??= (x) => true;

            IEnumerable<T> found = GameObject.FindObjectsOfType(typeof(T))?.Select(x => (T)x);

            if (found is null)
            {
                logger.LogDebug($"Failed to find any {kind}s");
                return;
            }

            logger.LogDebug($"Found {found.Count()} {kind}s");

            IEnumerable<T> selected = found.Where(selector);

            logger.LogDebug($"Selected {selected.Count()} {kind}s");

            RegisterMonobehaviours(selected, kind, ActiveChecker, dynamicObject);
        }

        private void RegisterMonobehaviours<T>(IEnumerable<T> objects, InteractableKind Kind = InteractableKind.none, Func<T, bool> ActiveChecker = null, bool dynamicObject = true) where T : MonoBehaviour
        {
            if (Kind == InteractableKind.none)
            {
                return;
            }

            foreach (var item in objects)
            {
                if (ActiveChecker == null)
                {
                    PurchaseInteraction interaction = item?.gameObject?.GetComponent<PurchaseInteraction>();

                    if (interaction != null)
                    {
                        RegisterObject(Kind, item.gameObject, interaction, (interaction) => interaction.available, dynamicObject);

                        continue;
                    }
                }

                RegisterObject(Kind, item.gameObject, item, ActiveChecker, dynamicObject);
            }
        }

        private void RegisterObject<T>(InteractableKind type, GameObject gameObject, T BackingObject, Func<T, bool> Expression, bool Dynamic)
        {
            ITrackedObject newObject = new TrackedObject<T>(type, gameObject, null)
            {
                BackingObject = BackingObject,
                ActiveChecker = Expression,
                DynamicObject = Dynamic
            };

            TrackedObjects.Add(newObject);

            TrackedDimensions.CheckValue(gameObject.transform.position);
        }
    }
}
