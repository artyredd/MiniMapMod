using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Interfaces
{
    public interface IConfig
    {
        IConfigEntry<T> Bind<T>(string section, string key, T defaultValue, string description);
    }
}
