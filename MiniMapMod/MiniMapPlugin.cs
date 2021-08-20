using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using MiniMapLibrary;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace MiniMapMod
{
    [BepInPlugin("MiniMap", "Mini Map Mod", "2.0.3")]
    public class MiniMapPlugin : BaseUnityPlugin
    {
        private readonly SpriteManager SpriteManager = new();

        private readonly List<ITrackedObject> TrackedObjects = new();

        private readonly Minimap Minimap = new();

        private float GlobalMinX;
        private float GlobalMaxX;
        private float GlobalMinZ;
        private float GlobalMaxZ;

        private float XDifference;
        private float XOffset;
        private float ZDifference;
        private float ZOffset;

        private bool Enable = false;

        private bool ScannedStaticObjects = false;

        public void Awake()
        {
            Log.Init(Logger);

            GlobalEventManager.onCharacterDeathGlobal += (x) => ScanScene();
            GlobalEventManager.OnInteractionsGlobal += (x, y, z) => ScanScene();
        }

        //The Update() method is run on every frame of the game.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                Enable = !Enable;

                if (Enable == false)
                {
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
                        ResetGlobalDimensions();

                        ScanScene();

                        CalculateMinimapConstraints();
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

            Transform parentTransform = objectivePanel.transform.Find("StripContainer");

            if (parentTransform == null)
            {
                Minimap.Destroy();
                return false;
            }

            Log.LogInfo("Creating Minimap");

            Minimap.CreateMinimap(this.SpriteManager, parentTransform.gameObject);

            Log.LogInfo("Finished creating Minimap");

            return true;
        }

        private void Reset()
        {
            TrackedObjects.Clear();
            ResetGlobalDimensions();
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

            RegisterTypes<ChestBehavior>(InteractableKind.Chest, dynamicObject: false);

            RegisterTypes<ShrineBloodBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterTypes<ShrineChanceBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterTypes<ShrineBossBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterTypes<ShrineCombatBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterTypes<ShrineHealingBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterTypes<ShrineRestackBehavior>(InteractableKind.Shrine, dynamicObject: false);

            RegisterTypes<ShopTerminalBehavior>(InteractableKind.Chest, dynamicObject: false);

            RegisterTypes<BarrelInteraction>(InteractableKind.Barrel, barrel => !barrel.Networkopened, dynamicObject: false);

            RegisterTypes<ScrapperController>(InteractableKind.Utility, dynamicObject: false);

            RegisterTypes<GenericInteraction>(InteractableKind.Special, dynamicObject: false);

            RegisterTypes<TeleporterInteraction>(InteractableKind.Teleporter, (teleporter) => teleporter.activationState != TeleporterInteraction.ActivationState.Charged, dynamicObject: false);

            RegisterTypes<SummonMasterBehavior>(InteractableKind.Drone, dynamicObject: false);

            ScannedStaticObjects = true;
        }

        private void ScanDynamicTypes()
        {
            RegisterTypes<AimAssistTarget>(InteractableKind.Enemy, x => true, dynamicObject: true);
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

        private void CalculateMinimapConstraints()
        {
            // set the values used to calculate the scaled positions in the minimap for the items

            // at this point the global mins and maxes are set determine the differences
            XDifference = GlobalMaxX - GlobalMinX;
            ZDifference = GlobalMaxZ - GlobalMinZ;

            // since the minimap uses a scale from 0d to 1d to position elements we should get the offsets for the x and z dimensions
            XOffset = -GlobalMinX;
            ZOffset = -GlobalMinZ;
        }

        private void RegisterTypes<T>(InteractableKind kind, Func<T, bool> ActiveChecker = null, bool dynamicObject = true) where T : MonoBehaviour
        {
            IEnumerable<T> found = GameObject.FindObjectsOfType(typeof(T)).Select(x => (T)x);

            RegisterTrackedObjects(found, kind, ActiveChecker, dynamicObject);
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

            x += XOffset;
            z += ZOffset;

            x /= XDifference;
            z /= ZDifference;

            return new(x * Settings.MinimapSize.Width, z * Settings.MinimapSize.Height);
        }

        private void RegisterTrackedObjects<T>(IEnumerable<T> objects, InteractableKind Kind = InteractableKind.none, Func<T, bool> ActiveChecker = null, bool dynamicObject = true) where T : MonoBehaviour
        {
            foreach (var item in objects)
            {
                if (Kind != InteractableKind.none)
                {
                    ITrackedObject newObject = null;

                    if (ActiveChecker == null)
                    {
                        PurchaseInteraction interaction = item.gameObject.GetComponent<PurchaseInteraction>();

                        if (interaction != null)
                        {
                            newObject = new TrackedObject<PurchaseInteraction>(Kind, item.gameObject, null)
                            {
                                BackingObject = interaction,
                                ActiveChecker = (interaction) => interaction.available,
                                DynamicObject = dynamicObject
                            };
                        }
                    }

                    if (newObject == null)
                    {
                        newObject = new TrackedObject<T>(Kind, item.gameObject, null)
                        {
                            BackingObject = item,
                            ActiveChecker = ActiveChecker,
                            DynamicObject = dynamicObject
                        };
                    }

                    TrackedObjects.Add(newObject);

                    CheckPositionConstraints(item.transform.position);
                }
            }
        }

        private void CheckPositionConstraints(Vector3 position)
        {
            if (position.x < GlobalMinX)
            {
                GlobalMinX = position.x;
            }
            else if (position.x > GlobalMaxX)
            {
                GlobalMaxX = position.x;
            }

            if (position.z < GlobalMinZ)
            {
                GlobalMinZ = position.z;
            }
            else if (position.z > GlobalMaxZ)
            {
                GlobalMaxZ = position.z;
            }
        }

        private void ResetGlobalDimensions()
        {
            GlobalMinX = float.MaxValue;
            GlobalMaxX = float.MinValue;

            GlobalMinZ = float.MaxValue;
            GlobalMaxZ = float.MinValue;

            ZOffset = 0;
            XOffset = 0;
        }
    }
}
