using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class WaitTask : ProgressTask
    {
        private readonly int Length, Count;
        private Timer Timer;
        private AutoResetEvent Event;

        public WaitTask(int length, int count) : base($"Pointlessly waiting for {length / 1000} seconds {count} time(s)")
        {
            Length = length;
            Count = count;
            Event = new AutoResetEvent(false);
            SetTotal(Length * Count);
        }

        public override Task StartAsync()
        {
            Timer = new Timer((state) => Event.Set(), Event, Length, Length);
            return new Task(() => Wait());
        }

        public void Wait()
        {
            for (int i = 0; i < Count; i++)
            {
                Event.WaitOne();
                ReportAdd(Length);
            }
        }
    }
}
