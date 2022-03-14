using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Interfaces
{
    public interface IConfigEntry<T>
    {
        T Value { get; }
    }
}
