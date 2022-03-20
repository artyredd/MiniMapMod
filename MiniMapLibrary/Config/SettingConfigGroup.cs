using MiniMapLibrary;
using MiniMapLibrary.Config;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Config
{
    public class SettingConfigGroup : ISettingConfigGroup
    {
        public SettingConfigGroup(IConfigEntry<bool> enabled, IConfigEntry<float> height, IConfigEntry<float> width, IConfigEntry<Color> activeColor, IConfigEntry<Color> inactiveColor, IConfigEntry<string> iconPath)
        {
            Enabled = enabled;
            Height = height;
            Width = width;
            ActiveColor = activeColor;
            InactiveColor = inactiveColor;
            IconPath = iconPath;
        }

        public SettingConfigGroup(IConfigEntry<bool> enabled, IConfigEntry<float> height, IConfigEntry<float> width, IConfigEntry<Color> activeColor, IConfigEntry<Color> inactiveColor, IConfigEntry<string> iconPath, IConfigEntry<bool> enabledElevationMarker, IConfigEntry<Vector2> elevationMarkerOffset, IConfigEntry<float> elevationMarkerHeight, IConfigEntry<float> elevationMarkerWidth, IConfigEntry<Color> elevationMarkerColor) : this(enabled, height, width, activeColor, inactiveColor, iconPath)
        {
            EnabledElevationMarker = enabledElevationMarker;
            ElevationMarkerOffset = elevationMarkerOffset;
            ElevationMarkerHeight = elevationMarkerHeight;
            ElevationMarkerWidth = elevationMarkerWidth;
            ElevationMarkerColor = elevationMarkerColor;
        }

        public IConfigEntry<bool> Enabled { get; }
        public IConfigEntry<float> Height { get; }
        public IConfigEntry<float> Width { get; }
        public IConfigEntry<Color> ActiveColor { get; }
        public IConfigEntry<Color> InactiveColor { get; }
        public IConfigEntry<string> IconPath { get; }
        public IConfigEntry<bool> EnabledElevationMarker { get; }
        public IConfigEntry<Vector2> ElevationMarkerOffset { get; }
        public IConfigEntry<float> ElevationMarkerHeight { get; }
        public IConfigEntry<float> ElevationMarkerWidth { get; }
        public IConfigEntry<Color> ElevationMarkerColor { get; }
    }
}
