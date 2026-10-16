using System;
using MiniMapLibrary;
using MiniMapLibrary.Scanner;
using UnityEngine;
using UnityEngine.UI;

namespace MiniMapMod
{
    internal sealed class MarkerRenderer
    {
        private readonly Minimap minimap;
        private readonly SpriteManager sprites;
        private readonly SceneTracker tracker;
        public int Frames { get; private set; }
        // Breaks Visible down further; costs a few native calls per icon, so debug only.
        public bool Diagnostics { get; set; }
        public int Tracked, Visible, Icons, Disabled, Hidden, Dead, Clipped, InvalidCoordinates;
        public int Transparent, MissingSprite, HierarchyHidden, CanvasCulled, Failures, Retrying;

        public MarkerRenderer(Minimap minimap, SpriteManager sprites, SceneTracker tracker)
        {
            this.minimap = minimap;
            this.sprites = sprites;
            this.tracker = tracker;
        }

        public void BeginFrame()
        {
            Frames++;
            Tracked = Visible = Icons = Disabled = Hidden = Dead = Clipped = InvalidCoordinates = 0;
            Transparent = MissingSprite = HierarchyHidden = CanvasCulled = Failures = Retrying = 0;
        }

        public void Reset() { BeginFrame(); Frames = 0; }

        public void Render(ReconciledSet<SceneMarker> markers, Vector3 viewerPosition, float scale)
        {
            float halfWidth = Settings.ViewfinderSize.Width * 0.5f;
            float halfHeight = Settings.ViewfinderSize.Height * 0.5f;
            for (int i = markers.Count - 1; i >= 0; i--)
            {
                SceneMarker marker = markers[i];
                Tracked++;
                if (!marker.Object || !marker.Source || !marker.WorldTransform)
                {
                    tracker.RemoveMarker(markers, i);
                    Dead++;
                    continue;
                }
                try
                {
                    if (Time.unscaledTime < marker.NextRenderAttempt) { Retrying++; Hide(marker); continue; }
                    if (!marker.Setting.Enabled) { Disabled++; Hide(marker); continue; }
                    if (!marker.DisplayAllowed || !marker.Source.isActiveAndEnabled) { Hidden++; Hide(marker); continue; }
                    if (marker.IsBody && marker.Health && !marker.Health.alive) { Dead++; Hide(marker); continue; }
                    Vector3 position = marker.WorldTransform.position;
                    if (!MapProjection.TryProject(position, viewerPosition, scale, minimap.RotationSine, minimap.RotationCosine, out Vector2 mapPosition))
                    {
                        InvalidCoordinates++;
                        Hide(marker);
                        continue;
                    }
                    float padding = Math.Max(marker.Setting.Dimensions.Width, marker.Setting.Dimensions.Height);
                    if (!MapProjection.IsInside(mapPosition, halfWidth, halfHeight, padding)) { Clipped++; Hide(marker); continue; }

                    RectTransform icon = marker.Icon.MinimapTransform;
                    if (!icon)
                    {
                        icon = minimap.CreateIcon(marker.Kind, mapPosition, sprites);
                        if (!icon) throw new InvalidOperationException("Icon factory returned no RectTransform for " + marker.Kind);
                        marker.Icon.MinimapTransform = icon;
                        marker.State.CheckActive();
                        marker.HasPosition = false;
                    }
                    if (!marker.DisplayAllowed) { Hidden++; Hide(marker); continue; }
                    if (!icon.gameObject.activeSelf)
                        icon.gameObject.SetActive(true);
                    if (!marker.HasPosition || (marker.LastPosition - mapPosition).sqrMagnitude > 0.0001f)
                    {
                        icon.anchoredPosition = mapPosition;
                        marker.LastPosition = mapPosition;
                        marker.HasPosition = true;
                    }
                    marker.Icon.UpdateElevation(viewerPosition.y, position.y, Settings.ElevationThreshold);
                    Image image = marker.State.MinimapImage;
                    if (!image) throw new InvalidOperationException("Icon has no Unity UI Image for " + marker.Kind);
                    if (marker.SpriteRevision != sprites.Revision)
                    {
                        image.sprite = sprites.GetSprite(marker.Kind);
                        marker.Icon.RefreshSprite();
                        marker.SpriteRevision = sprites.Revision;
                    }
                    Icons++;
                    if (!Diagnostics) Visible++;
                    else if (!image.sprite) MissingSprite++;
                    else if (image.color.a <= 0f) Transparent++;
                    else if (!icon.gameObject.activeInHierarchy) HierarchyHidden++;
                    else if (image.canvasRenderer.cull) CanvasCulled++;
                    else Visible++;
                }
                catch (Exception exception)
                {
                    Failures++;
                    // Back off so a broken icon doesn't throw every frame.
                    marker.NextRenderAttempt = Time.unscaledTime + 2f;
                    Hide(marker);
                    tracker.ReportFailure(marker.Source.GetType(), "render/icon", exception);
                }
            }
        }

        private static void Hide(SceneMarker marker)
        {
            RectTransform icon = marker.Icon.MinimapTransform;
            if (icon && icon.gameObject.activeSelf)
                icon.gameObject.SetActive(false);
        }

        public override string ToString() => $"frames={Frames} tracked={Tracked} icons={Icons} visible={Visible}"
            + $" filtered(disabled={Disabled},hidden={Hidden},dead={Dead},clipped={Clipped}) invalidCoordinates={InvalidCoordinates}"
            + $" transparent={Transparent} missingSprite={MissingSprite} hierarchyHidden={HierarchyHidden} canvasCulled={CanvasCulled} errors={Failures} retrying={Retrying}";
    }
}
