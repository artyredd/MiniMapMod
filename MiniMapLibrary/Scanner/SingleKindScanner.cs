using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#nullable enable
namespace MiniMapLibrary.Scanner
{
    public class SingleKindScanner<T> : ITrackedObjectScanner where T : MonoBehaviour
    {
        private readonly IScanner<T> scanner;
        private readonly bool dynamic;
        private readonly Func<T, bool> activeChecker;
        private readonly Func<T, GameObject> converter;
        private readonly Func<T, bool> selector;
        private readonly InteractableKind kind;
        private readonly Range3D range;

        public SingleKindScanner(InteractableKind kind, bool dynamic, IScanner<T> scanner, Range3D range, Func<T, GameObject> converter, Func<T, bool>? activeChecker = null, Func<T, bool>? selector = null)
        {
            this.scanner = scanner;
            this.dynamic = dynamic;
            this.kind = kind;
            this.converter = converter;

            // always return active if nothing is given
            this.activeChecker = activeChecker ?? ((x) => true);

            // always select if nothing is given
            this.selector = selector ?? ((x) => true);

            this.range = range;
        }

        public void ScanScene(IList<ITrackedObject> list)
        {
            IEnumerable<T> foundObjects = scanner.Scan();

            foreach (var item in foundObjects)
            {
                if (selector(item))
                {
                    GameObject gameObject = converter(item);

                    list.Add(new TrackedObject<T>(kind, converter(item), null)
                    {
                        BackingObject = item,
                        ActiveChecker = activeChecker,
                        DynamicObject = dynamic
                    });

                    range.CheckValue(gameObject.transform.position);
                }
            }
        }
    }
}
