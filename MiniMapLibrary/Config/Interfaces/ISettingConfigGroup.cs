using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Config
{
    public interface ISettingConfigGroup
    {
        IConfigEntry<bool> Enabled { get; }
        IConfigEntry<float> Height { get; }
        IConfigEntry<float> Width { get; }
        IConfigEntry<Color> ActiveColor { get; }
        IConfigEntry<Color> InactiveColor { get; }
        IConfigEntry<string> IconPath { get; }
    }
}
