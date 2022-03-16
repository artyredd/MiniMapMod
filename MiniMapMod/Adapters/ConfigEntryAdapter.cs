using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using MiniMapLibrary.Config;

namespace MiniMapMod.Adapters
{
    public class ConfigEntryAdapter<T> : IConfigEntry<T>
    {
        private readonly ConfigEntry<T> entry;

        public ConfigEntryAdapter(ConfigEntry<T> entry)
        {
            this.entry = entry;
        }

        public T Value => entry.Value;
    }
}
