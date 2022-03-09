using BepInEx;
using RoR2;
using UnityEngine;
using MiniMapLibrary;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MiniMapMod
{
    [BepInPlugin("MiniMap", "Mini Map Mod", "3.0.1")]
    public class MiniMapPlugin : BaseUnityPlugin
    {
        private readonly ISpriteManager SpriteManager = new SpriteManager();

        private readonly List<ITrackedObject> TrackedObjects = new();

        private readonly Minimap Minimap = new();

        private readonly Range3D TrackedDimensions = new();

        private bool Enable = true;

        private bool ScannedStaticObjects = false;

        public void Awake()
        {
            Log.Init(Logger);

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

            RegisterMonobehaviorType<ShopTerminalBehavior>(InteractableKind.Chest, dynamicObject: false);

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

        private void RegisterMonobehaviorType<T>(InteractableKind kind, Func<T, bool> ActiveChecker = null, bool dynamicObject = true) where T : MonoBehaviour
        {
            IEnumerable<T> found = GameObject.FindObjectsOfType(typeof(T)).Select(x => (T)x);

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
