using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

#nullable enable
namespace MiniMapLibrary.Scanner
{
    public class MonoBehaviourSorter<T> : IInteractibleSorter<T>
    {
        private readonly ISorter<T>[] sorters;

        public MonoBehaviourSorter(ISorter<T>[] sorters)
        {
            this.sorters = sorters;
        }

        public bool TrySort(T value, out InteractableKind kind, out GameObject? gameObject, out Func<T, bool> activeChecker)
        {
            kind = InteractableKind.none;
            gameObject = null;
            activeChecker = (x) => true;

            // iterate though our sorters
            foreach (ISorter<T> sorter in sorters)
            {
                // check if the provided value meets the criteria to be of this sorters
                // kind
                if (sorter.IsKind(value))
                {
                    // set the out kind and grab the gameobject
                    kind = sorter.Kind;
                    gameObject = sorter.GetGameObject(value);
                    activeChecker = sorter.CheckActive;

                    return true;
                }
            }

            return false;
        }
    }
}
