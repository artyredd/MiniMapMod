using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MiniMapLibrary
{
    public class Range3D
    {
        public Range1D X { get; set; } = new Range1D();

        public Range1D Y { get; set; } = new Range1D();

        public Range1D Z { get; set; } = new Range1D();

        public void CheckValue(Vector3 position)
        {
            X.CheckValue(position.x);
            //Y.CheckValue(position.y);
            Z.CheckValue(position.z);
        }

        public void Clear()
        {
            X.Clear();
            Y.Clear();
            Z.Clear();
        }
    }
}
