using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using MiniMapLibrary.Config;

namespace MiniMapMod.Adapters
{
    public class ConfigEntryAdapter<T> : IMutableConfigEntry<T>
    {
        private readonly ConfigEntry<T> entry;

        public ConfigEntryAdapter(ConfigEntry<T> entry)
        {
            this.entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        public T Value { get => entry.Value; set => entry.Value = value; }
    }
}
