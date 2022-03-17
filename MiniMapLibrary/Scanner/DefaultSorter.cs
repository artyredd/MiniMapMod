using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#nullable enable
namespace MiniMapLibrary.Scanner
{
    public class DefaultSorter<T> : ISorter<T>
    {
        public InteractableKind Kind { get; set; }

        private readonly Func<T, bool>? selector;
        private readonly Func<T, GameObject> converter;
        private readonly Func<T, bool>? activeChecker;

        public DefaultSorter(InteractableKind kind, 
            Func<T, GameObject> converter, 
            Func<T, bool>? selector = null, 
            Func<T, bool>? activeChecker = null)
        {
            Kind = kind;
            this.selector = selector;
            this.converter = converter;
            this.activeChecker = activeChecker;
        }

        public bool IsKind(T value) => selector?.Invoke(value) ?? false;

        public GameObject GetGameObject(T value) => converter(value);

        public bool CheckActive(T value) => activeChecker?.Invoke(value) ?? true;
    }
}
