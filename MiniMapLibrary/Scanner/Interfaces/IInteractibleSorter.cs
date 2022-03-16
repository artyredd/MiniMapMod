using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Scanner
{
    public interface IInteractibleSorter<T>
    {
        /// <summary>
        /// Attempts to find the applicable <see cref="InteractableKind"/> that the provided instance
        /// of <see cref="T"/> should be considered
        /// </summary>
        /// <param name="objectToBeSorted"></param>
        /// <returns></returns>
        bool TrySort(T objectToBeSorted, out InteractableKind kind);
    }
}
