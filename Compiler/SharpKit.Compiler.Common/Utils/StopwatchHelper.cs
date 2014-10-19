using System;
using System.Diagnostics;

namespace SharpKit.Compiler.Utils
{
    class StopwatchHelper
    {
        [DebuggerStepThrough]
        public static long TimeInMs(Action action)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }


    }
}
