using System;
using System.Collections.Generic;
using System.Text;

namespace MiniMapLibrary
{
    public class Range1D
    {
        public float Min { get; set; }

        public float Max { get; set; }

        /// <summary>
        /// Equivalent to <see cref="Range1D.Max"/> - <see cref="Min"/>
        /// </summary>
        public float Difference => Max - Min;

        /// <summary>
        /// Equivalent to -<see cref="Min"/>(neg)
        /// </summary>
        public float Offset => -Min;

        public void CheckValue(float value)
        {
            if (value < Min)
            {
                Min = value;
                return;
            }
            if (value > Max)
            {
                Max = value;
                return;
            }
        }

        public void Clear()
        {
            Min = float.MaxValue;
            Max = float.MinValue;
        }
    }
}
