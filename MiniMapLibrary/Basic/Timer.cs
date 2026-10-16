using System;

#nullable enable
namespace MiniMapLibrary
{
    public class Timer
    {
        public float Duration { get; }
        public float Value { get; set; }
        public event Action<object?>? OnFinished;
        public object? State { get; set; }
        public bool AutoReset { get; set; }
        public bool Started { get; set; } = true;
        public bool Expired => Value <= 0f;

        public Timer(float duration, bool autoReset = true)
        {
            if (!Numeric.IsFinite(duration) || duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));

            Duration = duration;
            AutoReset = autoReset;
            Value = duration;
        }

        public void Update(float deltaTime)
        {
            if (!Started || !Numeric.IsFinite(deltaTime) || deltaTime <= 0f)
                return;

            Value -= deltaTime;
            if (!Expired)
                return;

            if (AutoReset)
            {
                // Keep fractional time without firing repeatedly after a long frame.
                Value = Duration + Value % Duration;
                if (Value <= 0f)
                    Value = Duration;
            }
            else
            {
                Value = 0f;
                Started = false;
            }

            OnFinished?.Invoke(State);
        }

        public void Start() => Started = true;

        public void Reset()
        {
            Value = Duration;
            Started = AutoReset;
        }

        public override string ToString() => $"Timer [{Value}/{Duration}s] [AutoReset: {AutoReset}] [Started: {Started}]";
    }
}
