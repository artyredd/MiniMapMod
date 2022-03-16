using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Config
{
    public class StubConfigEntry<T> : IConfigEntry<T>
    {
        public T Value { get; }

        public StubConfigEntry(T value)
        {
            Value = value;
        }
    }
}
