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
        private readonly Func<T, bool>[] activeCheckers;
        private static readonly Func<T, bool> AlwaysActive = _ => true;

        public MonoBehaviourSorter(ISorter<T>[] sorters)
        {
            this.sorters = sorters ?? throw new ArgumentNullException(nameof(sorters));
            activeCheckers = new Func<T, bool>[sorters.Length];
            for (int i = 0; i < sorters.Length; i++)
                activeCheckers[i] = sorters[i].CheckActive;
        }

        public bool TrySort(T value, out InteractableKind kind, out GameObject? gameObject, out Func<T, bool> activeChecker)
        {
            kind = InteractableKind.none;
            gameObject = null;
            activeChecker = AlwaysActive;

            // iterate though our sorters
            for (int i = 0; i < sorters.Length; i++)
            {
                ISorter<T> sorter = sorters[i];
                // check if the provided value meets the criteria to be of this sorters
                // kind
                if (sorter.IsKind(value))
                {
                    // set the out kind and grab the gameobject
                    kind = sorter.Kind;
                    gameObject = sorter.GetGameObject(value);
                    activeChecker = activeCheckers[i];

                    return gameObject != null;
                }
            }

            return false;
        }
    }
}
