using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniMapLibrary.Scanner
{
    public class MonoBehaviorScanner<T> : IScanner<T> where T : MonoBehaviour
    {
        private readonly ILogger logger;

        public MonoBehaviorScanner(ILogger logger) => this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public IEnumerable<T> Scan()
        {
            T[] objects = UnityEngine.Object.FindObjectsOfType<T>();
            logger.LogDebug($"Found {objects.Length} objects of type {typeof(T).FullName}.");
            return objects;
        }
    }
}
