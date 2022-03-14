using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable
namespace MiniMapLibrary
{
    public sealed class SpriteManager : IDisposable, ISpriteManager
    {
        public const string DefaultResourcePath = "Textures/MiscIcons/texMysteryIcon";

        private readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        public void Dispose()
        {
            SpriteCache.Clear();
        }

        public Sprite? GetSprite(InteractableKind type)
        {
            if (type == InteractableKind.none)
            {
                return null;
            }

            string? path = Settings.GetSetting(type)?.Config?.IconPath?.Value;

            if (path != null)
            {
                return GetOrCache(path);

                throw new Exception($"MissingTextureException: Interactible.{type} does not have a registered texture path to load.");
            }

            throw new Exception($"MissingTextureException: Interactible.{type} does not have a registered texture path to load.");
        }

        private Sprite? GetOrCache(string Path)
        {
            if (SpriteCache.ContainsKey(Path))
            {
                return SpriteCache[Path];
            }

            Sprite? loaded = Resources.Load<Sprite>(Path);

            if (loaded != null)
            {
                SpriteCache.Add(Path, loaded);
            }

            return loaded;
        }
    }
}
