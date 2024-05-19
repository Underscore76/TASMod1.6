using System;
using System.Diagnostics;

namespace TASMod.System
{
    public class TASStopWatch : Stopwatch
    {
        public TimeSpan CurrentTimeSpan = new TimeSpan();
        public new TimeSpan Elapsed
        {
            get { return CurrentTimeSpan; }
        }

        public new long ElapsedMilliseconds
        {
            get { return CurrentTimeSpan.Ticks / TimeSpan.TicksPerMillisecond; }
        }

        public void Advance(TimeSpan span)
        {
            CurrentTimeSpan += span;
        }

        public new void Reset()
        {
            CurrentTimeSpan = new TimeSpan();
        }

        public TASStopWatch Clone()
        {
            return new TASStopWatch { CurrentTimeSpan = CurrentTimeSpan };
        }
    }
}
