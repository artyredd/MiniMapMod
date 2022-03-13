using BepInEx;
using RoR2;
using UnityEngine;
using MiniMapLibrary;
using System.Collections.Generic;
using System.Linq;
using System;
using BepInEx.Configuration;

namespace MiniMapMod
{
    [BepInPlugin("MiniMap", "Mini Map Mod", "3.1.0")]
    public class MiniMapPlugin : BaseUnityPlugin
    {
        private readonly ISpriteManager SpriteManager = new SpriteManager();

        private readonly List<ITrackedObject> TrackedObjects = new();

        private readonly Minimap Minimap = new();

        private readonly Range3D TrackedDimensions = new();

        private bool Enable = true;

        private bool ScannedStaticObjects = false;

        private readonly Dictionary<InteractableKind, ConfigEntry<bool>> ScanOptions = new();

        public void Awake()
        {
            Log.Init(Logger);

            // bind options
            InteractableKind[] kinds = Enum.GetValues(typeof(InteractableKind)).Cast<InteractableKind>().ToArray();

            for (int i = 0; i < kinds.Length; i++)
            {
                InteractableKind kind = kinds[i];
                
                ScanOptions.Add(kind, Config.Bind<bool>($"Icon.{kind}", "enabled", true, $"Whether or or {kind} should be shown on the minimap"));
                
                ConfigEntry<Color> activeColor = Config.Bind<Color>($"Icon.{kind}", "activeColor", Settings.GetColor(kind, true), "The color the icon should be when it has not been interacted with");
                ConfigEntry<Color> inactiveColor = Config.Bind<Color>($"Icon.{kind}", "inactiveColor", Settings.GetColor(kind, false), "The color the icon should be when it has used/bought");
                ConfigEntry<float> iconWidth = Config.Bind<float>($"Icon.{kind}", "width", Settings.GetInteractableSize(kind).Width, "Width of the icon");
                ConfigEntry<float> iconHeight = Config.Bind<float>($"Icon.{kind}", "height", Settings.GetInteractableSize(kind).Height, "Width of the icon");

                Settings.UpdateSetting(kind, iconWidth.Value, iconHeight.Value, activeColor.Value, inactiveColor.Value);
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
            if (ScannedStaticObjects)
            {
                return;
            }
            
            RegisterMonobehaviorType<ChestBehavior>(InteractableKind.Chest, dynamicObject: false);

            RegisterMonobehaviorType<ShrineBloodBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineChanceBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineBossBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineCombatBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineHealingBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterMonobehaviorType<ShrineRestackBehavior>(InteractableKind.Shrine, dynamicObject: false);

            // normal shops
            RegisterMonobehaviorType<ShopTerminalBehavior>(InteractableKind.Chest, dynamicObject: false, selector: shop => shop.GetComponent<PurchaseInteraction>().contextToken != "DUPLICATOR_CONTEXT");
            
            // duplicators
            RegisterMonobehaviorType<ShopTerminalBehavior>(InteractableKind.Printer, dynamicObject: false, selector: shop => shop.GetComponent<PurchaseInteraction>().contextToken == "DUPLICATOR_CONTEXT");

            RegisterMonobehaviorType<BarrelInteraction>(InteractableKind.Barrel, barrel => !barrel.Networkopened, dynamicObject: false);

            RegisterMonobehaviorType<ScrapperController>(InteractableKind.Utility, dynamicObject: false);

            RegisterMonobehaviorType<GenericInteraction>(InteractableKind.Special, dynamicObject: false);

            RegisterMonobehaviorType<TeleporterInteraction>(InteractableKind.Teleporter, (teleporter) => teleporter.activationState != TeleporterInteraction.ActivationState.Charged, dynamicObject: false);

            RegisterMonobehaviorType<SummonMasterBehavior>(InteractableKind.Drone, dynamicObject: false);

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
            if (ScanOptions[kind].Value == false)
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
