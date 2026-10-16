using System.Collections.Generic;

namespace MiniMapLibrary.Scanner
{
    internal static class ScannerUtilities
    {
        public static void CollectExistingIds(IList<ITrackedObject> objects, HashSet<int> ids)
        {
            ids.Clear();
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].gameObject)
                    ids.Add(objects[i].gameObject.GetInstanceID());
            }
        }
    }
}
