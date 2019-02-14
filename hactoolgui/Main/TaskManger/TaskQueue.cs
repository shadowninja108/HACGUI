using HACGUI.Main.TaskManger.Tasks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManger
{
    public class TaskQueue
    {
        public ConcurrentQueue<ProgressTask> Queue;

        public ProgressTask CurrentTask
        {
            get
            {
                if (Queue.TryPeek(out ProgressTask task))
                    return task;
                else
                    return null;
            }
        }
        public delegate void TaskEvent(ProgressTask task);

        public event TaskEvent TaskQueued, TaskStarted, TaskCompleted;

        public TaskQueue()
        {
            Queue = new ConcurrentQueue<ProgressTask>();
            RootWindow.Current.Submit(new Task(() => Loop()));
        }

        public void Submit(ProgressTask task)
        {
            Queue.Enqueue(task);
            TaskQueued(task);
        }

        public void Submit(IEnumerable<ProgressTask> tasks)
        {
            foreach (ProgressTask task in tasks)
                Submit(task);
        }

        public void Loop()
        {
            while (true)
            {
                if (Queue.TryDequeue(out ProgressTask task))
                {
                    TaskStarted(task);
                    task.StartAsync().RunSynchronously();
                    TaskCompleted(task);
                }
            }
        }
    }
}
