using MiniMapLibrary;
using RoR2;
using RoR2.Navigation;
using UnityEngine;

namespace MiniMapMod
{
    internal sealed class SceneBounds
    {
        private bool hasSceneBounds;
        public Range3D Range { get; } = new();
        public string Source { get; private set; } = "empty";

        public void Clear()
        {
            Range.Clear();
            hasSceneBounds = false;
            Source = "empty";
        }

        public void Refresh(SceneInfo scene, SceneTracker tracker, Vector3 viewerPosition)
        {
            if (Settings.WorldRadius > 0f)
            {
                Source = "configured radius";
                return;
            }
            if (hasSceneBounds)
                return;

            if (scene && scene.approximateMapBoundMesh)
            {
                Bounds bounds = scene.approximateMapBoundMesh.bounds;
                if (IsUsable(bounds.size))
                {
                    Range.Clear();
                    Range.CheckValue(bounds.min);
                    Range.CheckValue(bounds.max);
                    hasSceneBounds = true;
                    Source = "scene mesh";
                    return;
                }
            }

            NodeGraph nodes = scene ? scene.groundNodes : null;
            if (nodes && nodes.GetNodeCount() > 0)
            {
                Range.Clear();
                int count = nodes.GetNodeCount();
                for (int i = 0; i < count; i++)
                {
                    if (nodes.GetNodePosition(new NodeGraph.NodeIndex(i), out Vector3 position))
                        Range.CheckValue(position);
                }
                hasSceneBounds = Range.X.IsValid && Range.Z.IsValid;
                if (hasSceneBounds)
                {
                    Source = "navigation nodes";
                    return;
                }
            }

            // Modded stages may have neither. Use interactables only; enemies would keep changing the scale.
            if (!Range.X.IsValid || !Range.Z.IsValid)
                Range.CheckValue(viewerPosition);
            for (int i = 0; i < tracker.Interactables.Count; i++)
            {
                SceneMarker marker = tracker.Interactables[i];
                if (marker.Setting.Enabled && marker.DisplayAllowed && marker.WorldTransform)
                    Range.CheckValue(marker.WorldTransform.position);
            }
            Source = "discovered positions (minimum span 100)";
        }

        private static bool IsUsable(Vector3 size) => Numeric.IsFinite(size.x) && Numeric.IsFinite(size.z) && size.x > 1f && size.z > 1f;
    }
}
