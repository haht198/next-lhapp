using System;
using System.Threading;

namespace Common.Services.Static
{
    public class Interval
    {
        private readonly Action intervalAction;
        private readonly int intervalTime;
        private bool closed;
        private Thread intervalThread;
        public Interval(Action action, int _intervalTimeInMillisecond)
        {
            intervalAction = action;
            intervalTime = _intervalTimeInMillisecond;
            closed = true;
        }

        public Interval Run()
        {
            closed = false;
            intervalThread = new Thread(new ThreadStart(() => RunIntervalAction()));
            intervalThread.Start();
            return this;
        }

        public void Clear()
        {
            closed = true;
        }

        private void RunIntervalAction()
        {
            if (!closed && intervalAction != null)
            {
                intervalAction.Invoke();
                Thread.Sleep(intervalTime);
                RunIntervalAction();
            }
        }
    }
}
