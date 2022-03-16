using System;
using System.Collections.Generic;
using System.Text;
using MiniMapLibrary.Config;
using BepInEx.Configuration;
using BepInEx;

namespace MiniMapMod.Adapters
{
    public class ConfigAdapter : IConfig
    {
        private readonly ConfigFile plugin;

        public ConfigAdapter(ConfigFile plugin)
        {
            this.plugin = plugin;
        }

        public IConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description) => new ConfigEntryAdapter<T>(plugin.Bind<T>(section, key, defaultValue, description));
    }
}
