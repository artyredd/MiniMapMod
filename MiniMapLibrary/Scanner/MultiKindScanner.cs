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
        private readonly HashSet<int> existingIds = new();

        public MultiKindScanner(bool dynamic, IScanner<T> scanner, IInteractibleSorter<T> sorter, Range3D range, ISpriteManager spriteManager, Func<float> playerHeightRetriever)
        {
            this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            this.sorter = sorter ?? throw new ArgumentNullException(nameof(sorter));
            this.dynamic = dynamic;
            this.range = range ?? throw new ArgumentNullException(nameof(range));
            this.spriteManager = spriteManager ?? throw new ArgumentNullException(nameof(spriteManager));
            this.playerHeightRetriever = playerHeightRetriever ?? throw new ArgumentNullException(nameof(playerHeightRetriever));
        }

        public void ScanScene(IList<ITrackedObject> list)
        {
            IEnumerable<T> foundObjects = scanner.Scan();
            ScannerUtilities.CollectExistingIds(list, existingIds);

            foreach (var item in foundObjects)
            {
                if (item is null || (item is UnityEngine.Object source && !source))
                    continue;
                if (sorter.TrySort(item, out InteractableKind kind, out GameObject gameObject, out Func<T, bool> activeChecker))
                {
                    if (gameObject == null || !existingIds.Add(gameObject.GetInstanceID()))
                        continue;
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
