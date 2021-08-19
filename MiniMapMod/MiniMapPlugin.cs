using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using MiniMapLibrary;
using System.Collections.Generic;
using System.Linq;
using System;

namespace MiniMapMod
{
    [BepInPlugin("MiniMap", "Mini Map Mod", "2.0.0")]
    public class MiniMapPlugin : BaseUnityPlugin
    {
        private readonly SpriteManager SpriteManager = new();

        private readonly List<TrackedObject> TrackedObjects = new();

        private Minimap Minimap = new();

        private float GlobalMinX;
        private float GlobalMaxX;
        private float GlobalMinZ;
        private float GlobalMaxZ;

        private float XDifference;
        private float XOffset;
        private float ZDifference;
        private float ZOffset;

        private bool Enable = false;

        public void Awake()
        {
            Log.Init(Logger);

            GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
            GlobalEventManager.OnInteractionsGlobal += (x, y, z) => ScanScene();
        }

        private void OnCharacterDeath(object o)
        {
            Minimap.Destroy();
            ResetGlobalDimensions();
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
                }
            }

            if (Enable)
            {
                if (Minimap.Created)
                {
                    if (Camera.main == null)
                    {
                        Reset();
                        return;
                    }

                    try
                    {
                        Minimap.SetRotation(Camera.main.transform.rotation);
                    }
                    catch (NullReferenceException)
                    {
                        Reset();
                        return;
                    }

                    for (int i = 0; i < TrackedObjects.Count; i++)
                    {
                        var item = TrackedObjects[i];

                        if (item.MinimapTransform == null)
                        {
                            item.MinimapTransform = Minimap.CreateIcon(item.InteractableType, WorldToMinimap(item.gameObject.transform.position) - WorldToMinimap(Camera.main.transform.position), this.SpriteManager);
                        }
                        else
                        {
                            item.MinimapTransform.localPosition = WorldToMinimap(item.gameObject.transform.position) - WorldToMinimap(Camera.main.transform.position);
                        }
                    }

                    return;
                }

                Log.LogInfo("Creating Minimap");

                GameObject objectivePanel = GameObject.Find("ObjectivePanel");

                if (objectivePanel != null && this.SpriteManager != null)
                {
                    Transform parentTransform = objectivePanel.transform.Find("StripContainer");

                    if (parentTransform != null)
                    {
                        Minimap.CreateMinimap(this.SpriteManager, parentTransform.gameObject);

                        ScanScene();

                        Log.LogInfo("Finished creating Minimap");
                    }
                }
                else
                {
                    Minimap.Destroy();
                }
            }
        }

        private void Reset()
        {
            TrackedObjects.Clear();
            ResetGlobalDimensions();
            Minimap.Destroy();
        }

        private void ScanScene()
        {
            ResetGlobalDimensions();

            TrackedObjects.Clear();

            RegisterTypes(typeof(TeleporterInteraction), InteractableKind.Teleporter);

            RegisterTypes(typeof(ChestBehavior), InteractableKind.Chest);

            RegisterTypes(typeof(ShrineBloodBehavior), InteractableKind.Shrine);

            RegisterTypes(typeof(ShrineChanceBehavior), InteractableKind.Shrine);

            RegisterTypes(typeof(ShrineBossBehavior), InteractableKind.Shrine);

            RegisterTypes(typeof(ShrineCombatBehavior), InteractableKind.Shrine);

            RegisterTypes(typeof(ShrineHealingBehavior), InteractableKind.Shrine);

            RegisterTypes(typeof(ShrineRestackBehavior), InteractableKind.Shrine);

            RegisterTypes(typeof(ShopTerminalBehavior), InteractableKind.Chest);

            RegisterTypes(typeof(BarrelInteraction), InteractableKind.Utility);

            RegisterTypes(typeof(ScrapperController), InteractableKind.Utility);

            RegisterTypes(typeof(GenericInteraction), InteractableKind.Special);

            // set the values used to calculate the scaled positions in the minimap for the items

            // at this point the global mins and maxes are set determine the differences
            XDifference = GlobalMaxX - GlobalMinX;
            ZDifference = GlobalMaxZ - GlobalMinZ;

            // since the minimap uses a scale from 0d to 1d to position elements we should get the offsets for the x and z dimensions
            XOffset = -GlobalMinX;
            ZOffset = -GlobalMinZ;
        }

        private void RegisterTypes(Type type, InteractableKind kind)
        {
            IEnumerable<GameObject> found = GameObject.FindObjectsOfType(type).Select(x => ((MonoBehaviour)x).gameObject);

            RegisterObjects(found, kind);
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

        private void RegisterObjects(IEnumerable<GameObject> objects, InteractableKind Kind = InteractableKind.none)
        {
            foreach (var item in objects)
            {
                InteractableKind type = Kind;

                if (Kind == InteractableKind.none)
                {
                    type = Settings.GetInteractableType(item.name);
                }

                if (type != InteractableKind.none)
                {
                    TrackedObjects.Add(new TrackedObject(type, item, null));

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
        }
    }
}
