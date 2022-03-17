using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Scanner
{
    public interface ITrackedObjectScanner
    {
        /// <summary>
        /// Scans the scene for any objects that should be placed on the minimap
        /// add the found objects to the privided list
        /// </summary>
        void ScanScene(IList<ITrackedObject> list);
    }
}
