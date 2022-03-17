using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Scanner
{
    public interface IInteractibleSorter<T>
    {
        /// <summary>
        /// Attempts to find the applicable <see cref="InteractableKind"/> that the provided instance
        /// of <see cref="T"/> should be considered and returns that objects gameobject that should
        /// be tracked to update it's position on the minimap
        /// </summary>
        /// <param name="objectToBeSorted"></param>
        /// <returns></returns>
        bool TrySort(T objectToBeSorted, out InteractableKind kind, out GameObject gameObject, out Func<T, bool> activeChecker);
    }
}
