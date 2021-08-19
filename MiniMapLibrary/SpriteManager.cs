using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#nullable enable
namespace MiniMapLibrary
{
    public sealed class SpriteManager : IDisposable
    {
        public const string DefaultResourcePath = "Textures/MiscIcons/texMysteryIcon";

        private static readonly Dictionary<InteractableKind, string> s_ResourceDictionary = new Dictionary<InteractableKind, string>();

        private readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        static SpriteManager()
        {
            InitializeResources();
        }

        private static void InitializeResources()
        {
            void Add(InteractableKind type, string ResourcePath)
            {
                s_ResourceDictionary.Add(type, ResourcePath);
            }

            Add(InteractableKind.Primary, DefaultResourcePath);
            Add(InteractableKind.Shrine, DefaultResourcePath);
            Add(InteractableKind.Special, DefaultResourcePath);
            Add(InteractableKind.Teleporter, DefaultResourcePath);
            Add(InteractableKind.Chest, DefaultResourcePath);
            Add(InteractableKind.Utility, DefaultResourcePath);
            Add(InteractableKind.Player, DefaultResourcePath);
            Add(InteractableKind.All, DefaultResourcePath);
        }

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

            if (s_ResourceDictionary.TryGetValue(type, out string? path))
            {
                if (path != null)
                {
                    return GetOrCache(path);
                }
            }

            return null;
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
