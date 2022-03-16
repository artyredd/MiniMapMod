using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary.Scanner
{
    public interface IScanner<T>
    {
        /// <summary>
        /// Scans the scene and returns all instances of the type
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> Scan();
    }
}
