using System;
using UnityEngine;

namespace MiniMapLibrary
{
    /// <summary>
    /// Represents some object that should provide sprites to be displayed on the minimap
    /// </summary>
    public interface ISpriteManager : IDisposable
    {
        /// <summary>
        /// Uses the provided <see cref="InteractableKind"/> to generate/provide and return a sprite that should represent that kind in the minimap
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Sprite GetSprite(InteractableKind type);
    }
}