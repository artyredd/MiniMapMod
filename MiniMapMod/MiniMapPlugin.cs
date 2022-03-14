using BepInEx;
using RoR2;
using UnityEngine;
using MiniMapLibrary;
using System.Collections.Generic;
using System.Linq;
using System;
using BepInEx.Configuration;
using MiniMapMod.Adapters;
using MiniMapLibrary.Interfaces;

namespace MiniMapMod
{
    [BepInPlugin("MiniMap", "Mini Map Mod", "3.1.4")]
    public class MiniMapPlugin : BaseUnityPlugin
    {
        private readonly ISpriteManager SpriteManager = new SpriteManager();

        private readonly List<ITrackedObject> TrackedObjects = new();

        private readonly Minimap Minimap = new();

        private readonly Range3D TrackedDimensions = new();

        private bool Enable = true;

        private bool ScannedStaticObjects = false;

        private IConfig config;

        public void Awake()
        {
            Log.Init(Logger);

            config = new ConfigAdapter(this.Config);

            // bind options
            InteractableKind[] kinds = Enum.GetValues(typeof(InteractableKind)).Cast<InteractableKind>().Where(x=>x != InteractableKind.none && x != InteractableKind.All).ToArray();

            foreach (var item in kinds)
            {
                Settings.LoadConfigEntries(item, config);

                InteractibleSetting setting = Settings.GetSetting(item);
                Log.LogInfo($"MINIMAP: Loaded {item} config [{(setting.Config.Enabled.Value ? "enabled" : "disabled")}, {setting.ActiveColor}, {setting.InactiveColor}, ({setting.Dimensions.Width}x{setting.Dimensions.Height})]");
            }

            Log.LogInfo("MINIMAP: Creating scene scan hooks");

            GlobalEventManager.onCharacterDeathGlobal += (x) => ScanScene();
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
                    Log.LogInfo("MINIMAP: Resetting minimap");
                    Reset();
                    return;
                }
            }

            if (Enable)
            {
                // the main camera becomes null when the scene ends on death or quits
                if (Camera.main == null)
                {
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

                Vector2 itemMinimapPosition = WorldToMinimap(item.gameObject.transform.position) - WorldToMinimap(Camera.main.transform.position);

                if (item.MinimapTransform == null)
                {
                    item.MinimapTransform = Minimap.CreateIcon(item.InteractableType, itemMinimapPosition, this.SpriteManager);
                }
                else
                {
                    item.MinimapTransform.localPosition = itemMinimapPosition;
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

            Transform parentTransform = objectivePanel.transform;   

            Log.LogInfo("MINIMAP: Creating Minimap object");

            Minimap.CreateMinimap(this.SpriteManager, parentTransform.gameObject);

            Log.LogInfo("MINIMAP: Finished creating Minimap");

            return true;
        }

        private void Reset()
        {
            TrackedObjects.Clear();
            TrackedDimensions.Clear();
            Minimap.Destroy();

            // mark the scene as scannable again so we scan for chests etc..
            ScannedStaticObjects = false;
        }

        private void ScanScene()
        {
            ClearDynamicTrackedObjects();

            ScanStaticTypes();

            ScanDynamicTypes();
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
                    var token = chest.GetComponent<PurchaseInteraction>().contextToken;
                    return token != "LUNAR_CHEST_CONTEXT" && token.Contains("STEALTH") == false && token != "FAN_CONTEXT";
                });

            // lunar pods
            RegisterMonobehaviorType<ChestBehavior>(InteractableKind.LunarPod, dynamicObject: false,
                selector: chest => {
                    var token = chest.GetComponent<PurchaseInteraction>().contextToken;
                    return token == "LUNAR_CHEST_CONTEXT";
                });

            // fans
            RegisterMonobehaviorType<ChestBehavior>(InteractableKind.Special, dynamicObject: false,
                selector: chest => {
                    var token = chest.GetComponent<PurchaseInteraction>().contextToken;
                    return token == "FAN_CONTEXT";
                });

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
                selector: shop => shop.GetComponent<PurchaseInteraction>().contextToken != "DUPLICATOR_CONTEXT");
            
            // duplicators
            RegisterMonobehaviorType<ShopTerminalBehavior>(InteractableKind.Printer, dynamicObject: false, 
                selector: shop => shop.GetComponent<PurchaseInteraction>().contextToken == "DUPLICATOR_CONTEXT");

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
            // check to see if it's enabled in the config
            if (Settings.GetSetting(kind).Config.Enabled.Value == false)
            {
                return;
            }

            selector ??= (x) => true;

            IEnumerable<T> found = GameObject.FindObjectsOfType(typeof(T)).Select(x => (T)x).Where(selector);

            RegisterMonobehaviours(found, kind, ActiveChecker, dynamicObject);
        }

        /// <summary>
        /// Converts world positions to UI positons scaled between 0 and 1
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector2 WorldToMinimap(Vector3 position)
        {
            float x = position.x;

            float z = position.z;

            x += TrackedDimensions.X.Offset;
            z += TrackedDimensions.Z.Offset;

            x /= TrackedDimensions.X.Difference;
            z /= TrackedDimensions.Z.Difference;

            return new(x * Settings.MinimapSize.Width, z * Settings.MinimapSize.Height);
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
                    PurchaseInteraction interaction = item.gameObject.GetComponent<PurchaseInteraction>();

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
