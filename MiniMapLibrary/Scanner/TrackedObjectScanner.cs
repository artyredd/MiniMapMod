using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary.Scanner
{
    public class TrackedObjectScanner<T> : ITrackedObjectScanner
    {
        private readonly IScanner<T> scanner;
        private readonly IInteractibleSorter<T> sorter;
        private readonly bool dynamic;

        public TrackedObjectScanner(bool dynamic, IScanner<T> scanner, IInteractibleSorter<T> sorter)
        {
            this.scanner = scanner;
            this.sorter = sorter;
            this.dynamic = dynamic;
        }

        public void ScanScene(IList<ITrackedObject> list)
        {
            IEnumerable<T> foundObjects = scanner.Scan();

            foreach (var item in foundObjects)
            {
                if (sorter.TrySort(item, out InteractableKind kind, out GameObject gameObject, out Func<T, bool> activeChecker))
                {
                    list.Add(new TrackedObject<T>(kind, gameObject, null) {
                        BackingObject = item,
                        ActiveChecker = activeChecker,
                        DynamicObject = dynamic
                    });
                }
            }
        }
    }
}
