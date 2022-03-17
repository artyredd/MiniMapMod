using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

#nullable enable
namespace MiniMapLibrary.Scanner
{
    public class MonoBehaviorScanner<T> : IScanner<T> where T : MonoBehaviour
    {
        private readonly ILogger logger;

        public MonoBehaviorScanner(ILogger logger)
        {
            this.logger = logger;

            this.logger.LogDebug($"Created {nameof(MonoBehaviorScanner<T>)}<{typeof(T).FullName}>");
        }

        public IEnumerable<T> Scan()
        {
            IEnumerable<T>? found = GameObject.FindObjectsOfType(typeof(T))?.Select(x => (T)x);

            if (found is null)
            {
                logger.LogDebug($"Failed to find any objects with type {typeof(T).FullName}s");
            }
            else 
            {
                logger.LogDebug($"Found {found.Count()} objects with type {typeof(T).FullName}s");
            }

            // never return a null list, caller should always assume a list will
            // be provided regardless if nothing is found
            return found ?? new List<T>();
        }
    }
}
