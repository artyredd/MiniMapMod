using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Config
{
    public interface ISettingConfigGroup
    {
        IConfigEntry<bool> Enabled { get; }
        IConfigEntry<bool> EnabledElevationMarker { get; }
        IConfigEntry<Vector2> ElevationMarkerOffset { get; }
        IConfigEntry<float> ElevationMarkerHeight { get; }
        IConfigEntry<float> ElevationMarkerWidth { get; }
        IConfigEntry<Color> ElevationMarkerColor { get; }
        IConfigEntry<float> Height { get; }
        IConfigEntry<float> Width { get; }
        IConfigEntry<Color> ActiveColor { get; }
        IConfigEntry<Color> InactiveColor { get; }
        IConfigEntry<string> IconPath { get; }
    }
}
