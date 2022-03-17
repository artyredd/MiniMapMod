using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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

        /// <summary>
        /// Retrieves the gameobject from the provided T value
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        GameObject GetGameObject(T value);

        /// <summary>
        /// Checks if the provided value is active and should be displayed as active on the minimap
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        bool CheckActive(T value);
    }
}
