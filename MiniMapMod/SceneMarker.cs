using System;
using MiniMapLibrary;
using MiniMapLibrary.Scanner;
using RoR2;
using UnityEngine;

namespace MiniMapMod
{
    internal sealed class SceneMarker
    {
        public readonly GameObject Object;
        public readonly Transform WorldTransform;
        public readonly Behaviour Source;
        public readonly IInteractable Interaction;
        public readonly InteractableKind Kind;
        public readonly InteractibleSetting Setting;
        public readonly TrackedObject<SceneMarker> State;
        public readonly ElevationTrackedObject Icon;
        public readonly string Token;
        public readonly ChestBehavior Chest;
        public readonly bool UseScannerVisibility;
        public bool DisplayAllowed = true;
        public CharacterBody Body;
        public HealthComponent Health;
        public bool IsBody;
        public bool HasPosition;
        public Vector2 LastPosition;
        public int SpriteRevision = -1;
        public float NextRenderAttempt;

        public SceneMarker(GameObject gameObject, Behaviour source, IInteractable interaction, InteractableKind kind,
            ISpriteManager sprites, Func<float> viewerHeight, Func<SceneMarker, bool> activeChecker, MiniMapLibrary.ILogger logger,
            bool useScannerVisibility = false)
        {
            Object = gameObject;
            WorldTransform = gameObject.transform;
            Source = source;
            Interaction = interaction;
            Kind = kind;
            Setting = Settings.GetSetting(kind);
            UseScannerVisibility = useScannerVisibility;
            Token = source is PurchaseInteraction purchase ? purchase.contextToken
                : source is GenericInteraction generic ? generic.contextToken : null;
            Chest = gameObject.GetComponent<ChestBehavior>();
            State = new TrackedObject<SceneMarker>(kind, gameObject, null)
            {
                BackingObject = this,
                ActiveChecker = activeChecker,
                DynamicObject = true,
                ErrorLogger = logger
            };
            Icon = new ElevationTrackedObject(State, sprites, viewerHeight);
        }

        public bool SourceUnchanged => Source && Source.isActiveAndEnabled && WorldTransform
            && (Source is not PurchaseInteraction purchase || purchase.contextToken == Token)
            && (Source is not GenericInteraction generic || generic.contextToken == Token);
    }
}
