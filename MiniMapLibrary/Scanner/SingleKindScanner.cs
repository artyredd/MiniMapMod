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
        private readonly ISpriteManager spriteManager;
        private readonly Func<float> playerHeightRetriver;
        private readonly HashSet<int> existingIds = new();

        public SingleKindScanner(InteractableKind kind, bool dynamic, IScanner<T> scanner, Range3D range, ISpriteManager spriteManager, Func<float> playerHeightRetriever, Func <T, GameObject> converter, Func<T, bool>? activeChecker = null, Func<T, bool>? selector = null)
        {
            this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            this.dynamic = dynamic;
            this.kind = kind;
            this.converter = converter ?? throw new ArgumentNullException(nameof(converter));

            // always return active if nothing is given
            this.activeChecker = activeChecker ?? ((x) => true);

            // always select if nothing is given
            this.selector = selector ?? ((x) => true);

            this.range = range ?? throw new ArgumentNullException(nameof(range));
            this.spriteManager = spriteManager ?? throw new ArgumentNullException(nameof(spriteManager));
            this.playerHeightRetriver = playerHeightRetriever ?? throw new ArgumentNullException(nameof(playerHeightRetriever));
        }

        public void ScanScene(IList<ITrackedObject> list)
        {
            // if the kind is disabled, don't bother scanning
            InteractibleSetting setting = Settings.GetSetting(kind);
            if (!(setting.Config?.Enabled.Value ?? setting.Enabled))
            {
                return;
            }

            IEnumerable<T> foundObjects = scanner.Scan();
            ScannerUtilities.CollectExistingIds(list, existingIds);

            foreach (var item in foundObjects)
            {
                if (item != null && selector(item))
                {
                    GameObject gameObject = converter(item);
                    if (gameObject == null || !existingIds.Add(gameObject.GetInstanceID()))
                        continue;

                    list.Add(new ElevationTrackedObject(new TrackedObject<T>(kind, gameObject, null)
                    {
                        BackingObject = item,
                        ActiveChecker = activeChecker,
                        DynamicObject = dynamic
                    }, spriteManager, playerHeightRetriver));

                    range.CheckValue(gameObject.transform.position);
                }
            }
        }
    }
}
