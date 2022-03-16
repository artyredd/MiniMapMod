using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Scanner
{
    public class MonoBehaviourSorter<T> : IInteractibleSorter<T>
    {
        private readonly ISorter<T>[] sorters;

        public MonoBehaviourSorter(ISorter<T>[] sorters)
        {
            this.sorters = sorters;
        }

        public bool TrySort(T value, out InteractableKind kind)
        {
            kind = InteractableKind.none;

            // for eah nicer looking but slghtly slower
            foreach (ISorter<T> sorter in sorters)
            {
                if (sorter.IsKind(value))
                {
                    kind = sorter.Kind;
                    return true;
                }
            }

            return false;
        }
    }
}
