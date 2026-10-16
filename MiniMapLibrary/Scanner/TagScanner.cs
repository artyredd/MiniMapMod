using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMapLibrary.Scanner
{
    public class TagScanner : IScanner<GameObject>
    {
        private readonly string targetTag;
        private readonly ILogger logger;

        public TagScanner(InteractableKind kind, bool dynamic, string tag, ILogger logger,
            Func<GameObject, bool> selector = null, Func<GameObject, bool> activeChecker = null)
        {
            targetTag = tag ?? throw new ArgumentNullException(nameof(tag));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public IEnumerable<GameObject> Scan()
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(targetTag);
            logger.LogDebug($"Found {objects.Length} objects tagged {targetTag}.");
            return objects;
        }
    }
}
