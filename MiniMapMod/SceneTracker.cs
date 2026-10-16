using System;
using System.Collections.Generic;
using MiniMapLibrary;
using MiniMapLibrary.Scanner;
using RoR2;
using UnityEngine;

namespace MiniMapMod
{
    // Builds markers from the game's own registries. A slow scene search only
    // backs this up for objects (usually modded) that never register themselves.
    internal sealed class SceneTracker
    {
        private readonly Minimap minimap;
        private readonly ISpriteManager sprites;
        private readonly MiniMapLibrary.ILogger logger;
        private readonly Func<float> viewerHeight;
        private readonly Func<SceneMarker, bool> activeChecker;
        private readonly Action<SceneMarker> release;
        private readonly HashSet<(Type, string)> reportedFailures = new();
        private readonly HashSet<int> candidateIds = new();
        private readonly List<Behaviour> sceneInteractions = new();

        public ReconciledSet<SceneMarker> Interactables { get; } = new();
        public ReconciledSet<SceneMarker> Bodies { get; } = new();
        public ReconciledSet<SceneMarker> Pickups { get; } = new();
        public Interactor ViewerInteractor { get; set; }
        public ScanMetrics InteractionScan { get; } = new();
        public ScanMetrics BodyScan { get; } = new();
        public ScanMetrics PickupScan { get; } = new();
        public int SceneScans { get; private set; }
        public int SceneComponents { get; private set; }
        public int UnregisteredCandidates => sceneInteractions.Count;
        public double SceneScanMilliseconds { get; private set; }
        public int ActiveStates { get; private set; }
        public int HiddenStates { get; private set; }
        public int RepeatedErrors { get; private set; }

        public SceneTracker(Minimap minimap, ISpriteManager sprites, MiniMapLibrary.ILogger logger, Func<float> viewerHeight)
        {
            this.minimap = minimap;
            this.sprites = sprites;
            this.logger = logger;
            this.viewerHeight = viewerHeight;
            activeChecker = IsActive;
            release = Release;
        }

        public void RefreshSceneCandidates()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            candidateIds.Clear();
            try
            {
                List<IInteractable> registered = InterfaceInstanceTracker<IInteractable>.GetInstancesList();
                if (registered != null)
                {
                    for (int i = 0; i < registered.Count; i++)
                    {
                        if (registered[i] is Component component && component)
                            candidateIds.Add(component.gameObject.GetInstanceID());
                    }
                }
            }
            catch (Exception exception)
            {
                ReportFailure(typeof(IInteractable), "registry", exception);
            }
            if (TeleporterInteraction.instance)
                candidateIds.Add(TeleporterInteraction.instance.gameObject.GetInstanceID());

            // FindObjectsOfType only returns active scene objects; Resources.FindObjectsOfTypeAll would include prefabs.
            MonoBehaviour[] components = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
            sceneInteractions.Clear();
            SceneComponents = components.Length;
            SceneScans++;
            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if ((component is IInteractable or IChestBehavior or RouletteChestController
                    or ShrineBehavior or ScrapperController or SummonMasterBehavior or DronePurchaseRepair
                    or HalcyoniteShrineInteractable or ShrineRebirthController)
                    && IsSceneComponent(component) && !IsWorldPickup(component)
                    && !candidateIds.Contains(component.gameObject.GetInstanceID()))
                    sceneInteractions.Add(component);
            }
            candidateIds.Clear();
            watch.Stop();
            SceneScanMilliseconds = watch.Elapsed.TotalMilliseconds;
        }

        private static bool IsSceneComponent(Behaviour component) => component && component.isActiveAndEnabled
            && component.gameObject.scene.IsValid() && component.gameObject.scene.isLoaded;

        // Command cubes and void potentials are pickers carrying a PickupIndexNetworker;
        // scrappers, cleansing pools and other choice terminals have none.
        private static bool IsWorldPickup(Behaviour component) => component is GenericPickupController
            || (component is PickupPickerController && component.GetComponent<PickupIndexNetworker>());

        public void RefreshInteractables()
        {
            InteractionScan.Begin();
            candidateIds.Clear();
            Interactables.BeginScan();
            try
            {
                List<IInteractable> instances = InterfaceInstanceTracker<IInteractable>.GetInstancesList();
                InteractionScan.RegistryAvailable = instances != null;
                InteractionScan.RegistryCandidates = instances?.Count ?? 0;
                if (instances != null)
                {
                    for (int i = 0; i < instances.Count; i++)
                        TrackInteraction(instances[i] as Behaviour);
                }
            }
            catch (Exception exception)
            {
                InteractionScan.Errors++;
                ReportFailure(typeof(IInteractable), "registry", exception);
            }
            // Cached fallback objects take part in every refresh, otherwise EndScan would drop them.
            InteractionScan.SceneCandidates = sceneInteractions.Count;
            for (int i = sceneInteractions.Count - 1; i >= 0; i--)
            {
                if (!sceneInteractions[i])
                    sceneInteractions.RemoveAt(i);
                else
                    TrackInteraction(sceneInteractions[i]);
            }
            if (TeleporterInteraction.instance)
                TrackInteraction(TeleporterInteraction.instance);
            Interactables.EndScan(release);
            for (int i = Bodies.Count - 1; i >= 0; i--)
            {
                if (Bodies[i].Kind != InteractableKind.Player && HasPreferredInteraction(Bodies.GetKey(i)))
                    Bodies.RemoveAt(i, release);
            }
        }

        private void TrackInteraction(Behaviour component)
        {
            if (!IsSceneComponent(component))
            {
                InteractionScan.Inactive++;
                return;
            }
            if (component is GenericPickupController)
                return;
            try
            {
                GameObject gameObject = component.gameObject;
                int id = gameObject.GetInstanceID();
                if (!candidateIds.Add(id))
                {
                    InteractionScan.Duplicates++;
                    return;
                }
                IInteractable candidate = component as IInteractable;
                if (Interactables.TryGetAndMark(id, out SceneMarker marker)
                    && ReferenceEquals(marker.Object, gameObject) && marker.SourceUnchanged
                    && (marker.Interaction != null || candidate == null))
                {
                    InteractionScan.Unique++;
                    InteractionScan.Reused++;
                    if (!marker.Setting.Enabled)
                        InteractionScan.Disabled++;
                    return;
                }
                // Tracked separately by RefreshPickups.
                if (IsWorldPickup(component))
                    return;
                InteractionScan.Unique++;
                Classify(gameObject, component, candidate, out Behaviour source, out IInteractable interaction,
                    out InteractableKind kind, out bool useScannerVisibility);
                var next = new SceneMarker(gameObject, source, interaction, kind, sprites, viewerHeight, activeChecker, logger, useScannerVisibility);
                InteractionScan.Classified++;
                if (next.Setting.Enabled)
                    next.State.CheckActive();
                else
                    InteractionScan.Disabled++;
                Interactables.Replace(id, next, release);
            }
            catch (Exception exception)
            {
                InteractionScan.Errors++;
                ReportFailure(component.GetType(), "classify", exception);
            }
        }

        public void RefreshPickups()
        {
            PickupScan.Begin();
            candidateIds.Clear();
            Pickups.BeginScan();
            try
            {
                List<GenericPickupController> drops = InstanceTracker.GetInstancesList<GenericPickupController>();
                List<PickupPickerController> pickers = InstanceTracker.GetInstancesList<PickupPickerController>();
                PickupScan.RegistryAvailable = true;
                PickupScan.RegistryCandidates = drops.Count + pickers.Count;
                for (int i = 0; i < drops.Count; i++)
                    TrackPickup(drops[i], false);
                for (int i = 0; i < pickers.Count; i++)
                    TrackPickup(pickers[i], true);
            }
            catch (Exception exception)
            {
                PickupScan.Errors++;
                ReportFailure(typeof(GenericPickupController), "registry", exception);
            }
            Pickups.EndScan(release);
        }

        private void TrackPickup(Behaviour component, bool picker)
        {
            if (!IsSceneComponent(component)) { PickupScan.Inactive++; return; }
            try
            {
                GameObject gameObject = component.gameObject;
                int id = gameObject.GetInstanceID();
                if (Pickups.TryGetAndMark(id, out SceneMarker marker) && ReferenceEquals(marker.Source, component))
                {
                    candidateIds.Add(id);
                    PickupScan.Reused++;
                    return;
                }
                if (picker && !gameObject.GetComponent<PickupIndexNetworker>())
                    return;
                if (!candidateIds.Add(id)) { PickupScan.Duplicates++; return; }
                PickupScan.Unique++;
                var next = new SceneMarker(gameObject, component, (IInteractable)component, InteractableKind.Pickup,
                    sprites, viewerHeight, activeChecker, logger);
                if (next.Setting.Enabled)
                    next.State.CheckActive();
                else
                    PickupScan.Disabled++;
                Pickups.Replace(id, next, release);
                PickupScan.Classified++;
            }
            catch (Exception exception)
            {
                PickupScan.Errors++;
                ReportFailure(component.GetType(), "classify", exception);
            }
        }

        public void RefreshBodies(GameObject viewedObject)
        {
            BodyScan.Begin();
            candidateIds.Clear();
            Bodies.BeginScan();
            try
            {
                for (int teamIndex = (int)TeamIndex.Neutral; teamIndex < (int)TeamIndex.Count; teamIndex++)
                {
                    var members = TeamComponent.GetTeamMembers((TeamIndex)teamIndex);
                    if (members == null)
                        continue;
                    BodyScan.RegistryAvailable = true;
                    BodyScan.RegistryCandidates += members.Count;
                    for (int i = 0; i < members.Count; i++)
                        TrackBody(members[i], viewedObject);
                }
            }
            catch (Exception exception)
            {
                BodyScan.Errors++;
                ReportFailure(typeof(TeamComponent), "registry", exception);
            }
            Bodies.EndScan(release);
        }

        private void TrackBody(TeamComponent team, GameObject viewedObject)
        {
            if (!IsSceneComponent(team)) { BodyScan.Inactive++; return; }
            try
            {
                int id = team.gameObject.GetInstanceID();
                if (!candidateIds.Add(id)) { BodyScan.Duplicates++; return; }
                BodyScan.Unique++;
                if (team.teamIndex < TeamIndex.Neutral || team.teamIndex >= TeamIndex.Count) { BodyScan.InvalidTeam++; return; }
                CharacterBody body = team.body;
                HealthComponent health = body ? body.healthComponent : null;
                if (health && !health.alive) { BodyScan.Dead++; return; }
                if (team.gameObject == viewedObject) { BodyScan.Local++; return; }
                InteractableKind kind = ClassifyTeam(team.teamIndex, body && body.isPlayerControlled);
                if (!Settings.GetSetting(kind).Enabled) { BodyScan.Disabled++; return; }
                if (kind != InteractableKind.Player && HasPreferredInteraction(id)) { BodyScan.PreferredInteraction++; return; }
                if (Bodies.TryGetAndMark(id, out SceneMarker marker) && marker.Kind == kind && ReferenceEquals(marker.Source, team))
                {
                    marker.Body = body;
                    marker.Health = health;
                    BodyScan.Reused++;
                    return;
                }
                var next = new SceneMarker(team.gameObject, team, null, kind, sprites, viewerHeight, activeChecker, logger)
                {
                    IsBody = true, Body = body, Health = health
                };
                Bodies.Replace(id, next, release);
                BodyScan.Classified++;
            }
            catch (Exception exception)
            {
                BodyScan.Errors++;
                ReportFailure(team.GetType(), "classify", exception);
            }
        }

        private bool HasPreferredInteraction(int id)
        {
            if (!Interactables.TryGetValue(id, out SceneMarker interaction) || !interaction.Source || !interaction.Source.isActiveAndEnabled)
                return false;
            // Hidden chests must not leak back in as a neutral body marker.
            return interaction.Kind is InteractableKind.StealthChest or InteractableKind.TimedChest
                || (interaction.Setting.Enabled && interaction.DisplayAllowed);
        }

        internal static InteractableKind ClassifyTeam(TeamIndex team, bool isPlayer) => team switch
        {
            TeamIndex.Neutral => InteractableKind.Neutral,
            TeamIndex.Player => isPlayer ? InteractableKind.Player : InteractableKind.Minion,
            TeamIndex.Lunar => InteractableKind.EnemyLunar,
            TeamIndex.Void => InteractableKind.EnemyVoid,
            TeamIndex.EndOfTheCycle => InteractableKind.EnemyEndOfCycle,
            _ => InteractableKind.EnemyMonster
        };

        private static void Classify(GameObject gameObject, Behaviour component, IInteractable candidate,
            out Behaviour source, out IInteractable interaction, out InteractableKind kind, out bool useScannerVisibility)
        {
            source = component;
            interaction = candidate;
            useScannerVisibility = false;

            // One marker per GameObject: a chest can carry both a purchase and a picker, for example.
            if (gameObject.TryGetComponent(out TeleporterInteraction teleporter) && teleporter.isActiveAndEnabled)
            {
                source = teleporter;
                interaction = teleporter;
                kind = InteractableKind.Teleporter;
            }
            else if (gameObject.TryGetComponent(out TimedChestController timed) && timed.isActiveAndEnabled)
            {
                source = timed;
                interaction = timed;
                kind = InteractableKind.TimedChest;
            }
            else if (gameObject.TryGetComponent(out PurchaseInteraction purchase) && purchase.isActiveAndEnabled)
            {
                source = purchase;
                interaction = purchase;
                kind = PurchaseTokenClassifier.Classify(purchase.contextToken, purchase.isShrine, purchase.isDrone);
                if (gameObject.TryGetComponent(out PortalStatueBehavior statue) && statue.portalType == PortalStatueBehavior.PortalType.Shop)
                    kind = InteractableKind.NewtAltar;
                else if (kind == InteractableKind.Special)
                {
                    if (gameObject.GetComponent<IChestBehavior>() != null || gameObject.GetComponent<RouletteChestController>())
                        kind = InteractableKind.Chest;
                    else if (gameObject.GetComponent<SummonMasterBehavior>() || gameObject.GetComponent<DronePurchaseRepair>())
                        kind = InteractableKind.Drone;
                    else if (gameObject.GetComponent<ShrineBehavior>() || gameObject.GetComponent<HalcyoniteShrineInteractable>()
                        || gameObject.GetComponent<ShrineRebirthController>())
                        kind = InteractableKind.Shrine;
                }
            }
            else if (component is IChestBehavior or RouletteChestController)
                kind = InteractableKind.Chest;
            else if (component is ScrapperController)
                kind = InteractableKind.Utility;
            else if (component is ShrineBehavior or HalcyoniteShrineInteractable or ShrineRebirthController)
                kind = InteractableKind.Shrine;
            else if (component is SummonMasterBehavior or DronePurchaseRepair)
                kind = InteractableKind.Drone;
            else if (component is BarrelInteraction)
                kind = InteractableKind.Barrel;
            // TieredPickupPickerController is the Alloyed Collective drone assembly station, not a drop.
            else if (component is TieredPickupPickerController or DroneScrapperPickerController or DroneCombinerController or ShrineCleanseBehavior)
                kind = InteractableKind.Utility;
            else if (component is PickupPickerController picker)
            {
                kind = PurchaseTokenClassifier.Classify(picker.contextString);
                if (kind == InteractableKind.Special)
                    kind = gameObject.GetComponent<ScrapperController>() ? InteractableKind.Utility : InteractableKind.Item;
            }
            else if (component is GenericInteraction generic)
            {
                kind = PurchaseTokenClassifier.Classify(generic.contextToken);
                if (kind == InteractableKind.Special && gameObject.GetComponent<HalcyoniteShrineInteractable>())
                    kind = InteractableKind.Shrine;
            }
            else if (component is TrialGongInteraction)
                kind = InteractableKind.Portal;
            else if (component is GrantHexesController)
                kind = InteractableKind.Shrine;
            else if (component is PowerPedestal)
                kind = InteractableKind.Totem;
            else
            {
                kind = InteractableKind.Special;
                useScannerVisibility = interaction != null;
            }
        }

        private bool IsActive(SceneMarker marker)
        {
            if (!marker.Source || !marker.Source.isActiveAndEnabled)
                return false;
            if (marker.IsBody)
                return !marker.Health || marker.Health.alive;

            try
            {
                if (marker.Kind == InteractableKind.Pickup)
                {
                    // Not GetInteractability: it reports Disabled for the first half second
                    // after a drop lands and for players who can't take that pickup.
                    marker.DisplayAllowed = marker.Source switch
                    {
                        GenericPickupController drop => drop.pickup.pickupIndex.isValid,
                        PickupPickerController picker => picker.available,
                        _ => true
                    };
                    return marker.DisplayAllowed;
                }
                marker.DisplayAllowed = !marker.UseScannerVisibility
                    || (Settings.ShowUnclassifiedInteractables && marker.Interaction.ShouldShowOnScanner());
                return marker.DisplayAllowed && marker.Source switch
                {
                    PurchaseInteraction purchase => purchase.available && (!marker.Chest || !marker.Chest.isChestOpened),
                    TeleporterInteraction teleporter => !teleporter.isCharged,
                    BarrelInteraction barrel => !barrel.Networkopened,
                    TimedChestController timed => Run.instance && PurchaseTokenClassifier.TimedChestAvailable(timed.purchased, timed.lockTime, Run.instance.GetRunStopwatch()),
                    GenericInteraction generic => generic.interactability == Interactability.Available,
                    PickupPickerController picker => picker.available,
                    _ => marker.Chest ? !marker.Chest.isChestOpened
                        : marker.Interaction == null || !ViewerInteractor || marker.Interaction.GetInteractability(ViewerInteractor) == Interactability.Available
                };
            }
            catch (Exception exception)
            {
                marker.DisplayAllowed = false;
                ReportFailure(marker.Source.GetType(), "state", exception);
                return false;
            }
        }

        public void UpdateStates()
        {
            ActiveStates = HiddenStates = 0;
            UpdateStates(Interactables);
            UpdateStates(Bodies);
            UpdateStates(Pickups);
        }

        private void UpdateStates(ReconciledSet<SceneMarker> markers)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                SceneMarker marker = markers[i];
                if (marker.Setting.Enabled)
                {
                    marker.State.CheckActive();
                    if (marker.State.Active) ActiveStates++;
                    if (!marker.DisplayAllowed) HiddenStates++;
                }
            }
        }

        public void DetachIcons()
        {
            DetachIcons(Interactables);
            DetachIcons(Bodies);
            DetachIcons(Pickups);
        }

        private void DetachIcons(ReconciledSet<SceneMarker> markers)
        {
            for (int i = 0; i < markers.Count; i++)
                Release(markers[i]);
        }

        private void Release(SceneMarker marker)
        {
            minimap.ReleaseIcon(marker.Kind, marker.Icon.MinimapTransform);
            marker.Icon.MinimapTransform = null;
            marker.HasPosition = false;
            marker.SpriteRevision = -1;
        }

        public void Clear()
        {
            Interactables.Clear(release);
            Bodies.Clear(release);
            Pickups.Clear(release);
            ViewerInteractor = null;
            reportedFailures.Clear();
            sceneInteractions.Clear();
            candidateIds.Clear();
            SceneScans = SceneComponents = ActiveStates = HiddenStates = RepeatedErrors = 0;
            SceneScanMilliseconds = 0;
            InteractionScan.Begin();
            BodyScan.Begin();
            PickupScan.Begin();
            InteractionScan.Invocations = BodyScan.Invocations = PickupScan.Invocations = 0;
        }

        public void RemoveMarker(ReconciledSet<SceneMarker> markers, int index) => markers.RemoveAt(index, release);

        internal void ReportFailure(Type type, string stage, Exception exception)
        {
            if (reportedFailures.Add((type, stage)))
                logger.LogException(exception, $"MiniMap {stage} failed for {type.FullName}. Other markers continue; repeat counts appear in DEBUG diagnostics.");
            else
                RepeatedErrors++;
        }
    }
}
