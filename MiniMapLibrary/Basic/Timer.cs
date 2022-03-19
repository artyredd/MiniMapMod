using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace MiniMapLibrary
{
    public class Timer
    {
        public float Duration { get; private set; } = 1.0f;
        public float Value { get; set; }
        public event Action<object?>? OnFinished;
        public object? State { get; set; }
        public bool AutoReset { get; set; }
        public bool Started { get; set; } = true;
        public bool Expired => Value <= 0;

        public Timer(float duration, bool autoReset = true)
        {
            Duration = duration;
            this.AutoReset = autoReset;
        }

        public void Update(float deltaTime)
        {
            if (Started)
            {
                Value -= deltaTime;

                if (Expired)
                {
                    if (AutoReset)
                    {
                        Reset();
                    }
                    else 
                    {
                        Started = false;
                    }

                    OnFinished?.Invoke(State);
                }
            }
        }

        public void Start() => Started = true;

        public void Reset()
        {
            Value = Duration;
            Started = AutoReset;
        }

        public override string ToString()
        {
            return $"{typeof(Timer).Name} [{Value}/{Duration}s] [{nameof(AutoReset)}: {AutoReset}] [{nameof(Started)}: {Started}] [{nameof(Expired)}: {Expired}]";
        }
    }
}
