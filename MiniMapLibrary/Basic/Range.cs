namespace MiniMapLibrary
{
    public class Range1D
    {
        public float Min { get; set; } = float.PositiveInfinity;
        public float Max { get; set; } = float.NegativeInfinity;

        public bool IsValid => Numeric.IsFinite(Min) && Numeric.IsFinite(Max) && Min <= Max;
        public float Difference => IsValid ? Max - Min : 0f;
        public float Offset => IsValid ? -Min : 0f;

        public void CheckValue(float value)
        {
            if (!Numeric.IsFinite(value))
                return;

            if (value < Min)
                Min = value;
            if (value > Max)
                Max = value;
        }

        public void Clear()
        {
            Min = float.PositiveInfinity;
            Max = float.NegativeInfinity;
        }
    }
}
