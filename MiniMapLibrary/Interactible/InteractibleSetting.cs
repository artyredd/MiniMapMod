using MiniMapLibrary.Config;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary
{
    public class InteractibleSetting
    {
        public Dimension2D Dimensions { get; set; }
        public Color ActiveColor { get; set; }
        public Color InactiveColor { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public Dimension2D ElevationMarkerOffset { get; set; }
        public Dimension2D ElevationMarkerDimensions { get; set; }
        public bool ElevationMarkerEnabled { get; set; }
        public Color ElevationMarkerColor { get; set; }
        public ISettingConfigGroup Config;
    }
}
