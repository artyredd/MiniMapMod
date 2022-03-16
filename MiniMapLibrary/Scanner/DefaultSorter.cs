using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace MiniMapLibrary.Scanner
{
    public class DefaultSorter<T> : ISorter<T>
    {
        public InteractableKind Kind { get; set; }

        private readonly Func<T, bool>? selector;

        public DefaultSorter(InteractableKind kind, Func<T, bool>? selector = null)
        {
            Kind = kind;
            this.selector = selector;
        }

        public bool IsKind(T value) => selector?.Invoke(value) ?? false;
    }
}
