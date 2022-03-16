using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Config
{
    public interface IConfigEntry<T>
    {
        T Value { get; }
    }
}
