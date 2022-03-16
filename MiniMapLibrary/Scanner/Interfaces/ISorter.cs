using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Scanner
{
    public interface ISorter<T>
    {
        InteractableKind Kind { get; }

        /// <summary>
        /// Checks to see if the value should be of Kind
        /// </summary>
        /// <param name="value"></param>
        /// <returns>true when the provided <paramref name="value"/> should be <see cref="Kind"/></returns>
        bool IsKind(T value);
    }
}
