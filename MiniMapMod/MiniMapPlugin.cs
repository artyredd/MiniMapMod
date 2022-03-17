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
    [BepInPlugin("MiniMap", "Mini Map Mod", "3.2.0")]
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

        private ITrackedObjectScanner[] staticScanners;
        private ITrackedObjectScanner[] dynamicScanners;

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

            Settings.LoadApplicationSettings(logger, config);

            // bind options
            InteractableKind[] kinds = Enum.GetValues(typeof(InteractableKind)).Cast<InteractableKind>().Where(x => x != InteractableKind.none && x != InteractableKind.All).ToArray();

            foreach (var item in kinds)
            {
                Settings.LoadConfigEntries(item, config);
            }

            logger.LogInfo("Creating scene scan hooks");

            // fill the scanner arrays
            CreateStaticScanners();

            CreateDynamicScanners();

            // hook events so the minimaps updates
            // scan scene should NEVER throw exceptions
            // doing so prevents all other subscribing events to not fire (after the exception)

            // this will re-scan the scene every time any npc, player dies
            GlobalEventManager.onCharacterDeathGlobal += (x) => ScanScene();

            // this will re-scan when the player uses anything like a chest
            // or the landing pod
            GlobalEventManager.OnInteractionsGlobal += (x, y, z) => ScanScene();
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
                        // we'll encounter null references when other mods or the game itself
                        // destroys entities we are tracking at runtime
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

            logger.LogInfo("Creating Minimap object");

            Minimap.CreateMinimap(this.SpriteManager, objectivePanel.gameObject);

            logger.LogInfo("Finished creating Minimap");

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

        private ITrackedObjectScanner CreateChestScanner(Func<ChestBehavior, bool> activeChecker)
        {
            bool TryGetPurchaseToken<T>(T value, out string out_token) where T : MonoBehaviour
            {
                var token = value?.GetComponent<PurchaseInteraction>()?.contextToken;

                // mods that implement ChestBehaviour, may not also PurchaseInteraction
                if (token is null)
                {
                    logger.LogDebug($"No {nameof(PurchaseInteraction)} component on {typeof(T).Name}. GameObject.name = {value.gameObject.name}");
                }

                out_token = token;

                return token != null;
            }

            // given any chest object this will retur true if it should be displayed as a chest
            bool ChestSelector(ChestBehavior chest)
            {
                if (TryGetPurchaseToken(chest, out string token))
                {
                    return token.Contains("CHEST") && token != "LUNAR_CHEST_CONTEXT" && token.Contains("STEALTH") == false;
                }

                return false;
            }

            bool LunarPodSelector(ChestBehavior chest)
            {
                if (TryGetPurchaseToken(chest, out string token))
                {
                    return token == "LUNAR_CHEST_CONTEXT";
                }

                return false;
            }

            return new MultiKindScanner<ChestBehavior>(false,
                new MonoBehaviorScanner<ChestBehavior>(logger), new MonoBehaviourSorter<ChestBehavior>(
                    new ISorter<ChestBehavior>[] {
                        new DefaultSorter<ChestBehavior>(InteractableKind.Chest, x => x.gameObject, ChestSelector, activeChecker),
                        new DefaultSorter<ChestBehavior>(InteractableKind.LunarPod,  x => x.gameObject, LunarPodSelector, activeChecker),
                    }
                ), TrackedDimensions);
        }

        private ITrackedObjectScanner CreatePurchaseInteractionScanner()
        {
            bool FanSelector(PurchaseInteraction interaction) => interaction?.contextToken == "FAN_CONTEXT";

            bool PrinterSelector(PurchaseInteraction interaction) => interaction?.contextToken?.Contains("DUPLICATOR") ?? false;

            bool ShopSelector(PurchaseInteraction interaction) => interaction?.contextToken?.Contains("TERMINAL") ?? false;

            bool EquipmentSelector(PurchaseInteraction interaction) => interaction?.contextToken?.Contains("EQUIP") ?? false;

            bool InteractionActiveChecker(PurchaseInteraction interaction) => interaction?.available ?? true;

            GameObject DefaultConverter<T>(T value) where T : MonoBehaviour => value?.gameObject;

            return new MultiKindScanner<PurchaseInteraction>(false,
                new MonoBehaviorScanner<PurchaseInteraction>(logger), new MonoBehaviourSorter<PurchaseInteraction>(
                    new ISorter<PurchaseInteraction>[] {
                        new DefaultSorter<PurchaseInteraction>(InteractableKind.Printer, DefaultConverter, PrinterSelector, InteractionActiveChecker),
                        new DefaultSorter<PurchaseInteraction>(InteractableKind.Special, DefaultConverter, FanSelector, InteractionActiveChecker),
                        new DefaultSorter<PurchaseInteraction>(InteractableKind.Shop, DefaultConverter, ShopSelector, InteractionActiveChecker),
                        new DefaultSorter<PurchaseInteraction>(InteractableKind.Equipment, DefaultConverter, EquipmentSelector, InteractionActiveChecker),
                    }
                ), TrackedDimensions);
        }

        private void CreateStaticScanners()
        {
            GameObject DefaultConverter<T>(T value) where T: MonoBehaviour => value?.gameObject;

            bool DefaultActiveChecker<T>(T value) where T: MonoBehaviour
            {
                if (value is PurchaseInteraction isInteraction)
                {
                    return isInteraction.available;
                }

                PurchaseInteraction interaction = value?.GetComponent<PurchaseInteraction>();

                if (interaction != null) 
                {
                    return interaction.available;
                }

                // default always active;
                return true;
            }

            ITrackedObjectScanner SimpleScanner<T>(InteractableKind kind, Func<T, bool> activeChecker = null, Func<T, bool> selector = null, Func<T, GameObject> converter = null) where T: MonoBehaviour
            {
                return new SingleKindScanner<T>(
                    kind: kind,
                    dynamic: false,
                    scanner: new MonoBehaviorScanner<T>(logger),
                    range: TrackedDimensions,
                    converter: converter ?? DefaultConverter,
                    activeChecker: activeChecker ?? DefaultActiveChecker,
                    selector: selector
                );
            }

            staticScanners = new ITrackedObjectScanner[] {
                CreateChestScanner(DefaultActiveChecker),
                CreatePurchaseInteractionScanner(),
                SimpleScanner<RouletteChestController>(InteractableKind.Chest),
                SimpleScanner<ShrineBloodBehavior>(InteractableKind.Shrine),
                SimpleScanner<ShrineChanceBehavior>(InteractableKind.Shrine),
                SimpleScanner<ShrineCombatBehavior>(InteractableKind.Shrine),
                SimpleScanner<ShrineHealingBehavior>(InteractableKind.Shrine),
                SimpleScanner<ShrineRestackBehavior>(InteractableKind.Shrine),
                SimpleScanner<ScrapperController>(InteractableKind.Utility),
                SimpleScanner<TeleporterInteraction>(InteractableKind.Teleporter, activeChecker: (teleporter) => teleporter.activationState != TeleporterInteraction.ActivationState.Charged),
                SimpleScanner<SummonMasterBehavior>(InteractableKind.Drone),
                SimpleScanner<GenericInteraction>(InteractableKind.Special),
                SimpleScanner<BarrelInteraction>(InteractableKind.Barrel, activeChecker: barrel => !barrel.Networkopened),
            };
        }

        private void CreateDynamicScanners()
        {
            dynamicScanners = new ITrackedObjectScanner[] {
                new SingleKindScanner<AimAssistTarget>(InteractableKind.Enemy, true, 
                    scanner: new MonoBehaviorScanner<AimAssistTarget>(logger), 
                    range: TrackedDimensions, 
                    converter: x => x.gameObject, 
                    activeChecker: x => true
                )
            };
        }

        private void ScanStaticTypes()
        {
            // if we have alreadys scanned don't scan again until we die or the scene changes (this method has sever performance implications)
            if (ScannedStaticObjects)
            {
                return;
            }

            for (int i = 0; i < staticScanners.Length; i++)
            {
                staticScanners[i].ScanScene(TrackedObjects);
            }

            ScannedStaticObjects = true;
        }

        private void ScanDynamicTypes()
        {
            for (int i = 0; i < dynamicScanners.Length; i++)
            {
                dynamicScanners[i].ScanScene(TrackedObjects);
            }
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
    }
}
