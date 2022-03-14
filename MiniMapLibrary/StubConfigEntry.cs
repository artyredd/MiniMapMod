using MiniMapLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary
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
