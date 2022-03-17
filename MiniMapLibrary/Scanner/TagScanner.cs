using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#nullable enable
namespace MiniMapLibrary.Scanner
{
    public class TagScanner : IScanner<GameObject>
    {
        private readonly string targetTag;
        private readonly ILogger logger;

        public TagScanner(InteractableKind kind, bool dynamic, string tag, ILogger logger, Func<GameObject, bool>? selector = null, Func<GameObject, bool>? activeChecker = null)
        {
            this.targetTag = tag;
            this.logger = logger;

            logger.LogDebug($"Created {nameof(TagScanner)} for tag {tag}");
        }

        public IEnumerable<GameObject> Scan()
        {
            IEnumerable<GameObject>? found = GameObject.FindGameObjectsWithTag(targetTag);

            if (found is null)
            {
                logger.LogDebug($"Failed to find any objects with tag {targetTag}");
            }
            else
            {
                logger.LogDebug($"Found {found.Count()} objects with tag {targetTag}");
            }

            return found ?? new List<GameObject>();
        }
    }
}
