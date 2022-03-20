using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Scanner
{
    public class MultiKindScanner<T> : ITrackedObjectScanner
    {
        private readonly IScanner<T> scanner;
        private readonly IInteractibleSorter<T> sorter;
        private readonly bool dynamic;
        private readonly Range3D range;
        private readonly ISpriteManager spriteManager;
        private readonly Func<float> playerHeightRetriever;

        public MultiKindScanner(bool dynamic, IScanner<T> scanner, IInteractibleSorter<T> sorter, Range3D range, ISpriteManager spriteManager, Func<float> playerHeightRetriever)
        {
            this.scanner = scanner;
            this.sorter = sorter;
            this.dynamic = dynamic;
            this.range = range;
            this.spriteManager = spriteManager;
            this.playerHeightRetriever = playerHeightRetriever;
        }

        public void ScanScene(IList<ITrackedObject> list)
        {
            IEnumerable<T> foundObjects = scanner.Scan();

            foreach (var item in foundObjects)
            {
                if (sorter.TrySort(item, out InteractableKind kind, out GameObject gameObject, out Func<T, bool> activeChecker))
                {
                    list.Add(new ElevationTrackedObject(new TrackedObject<T>(kind, gameObject, null)
                    {
                        BackingObject = item,
                        ActiveChecker = activeChecker,
                        DynamicObject = dynamic
                    }, spriteManager, playerHeightRetriever));

                    range.CheckValue(gameObject.transform.position);
                }
            }
        }
    }
}
