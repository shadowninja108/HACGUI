using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    // On god?
    public class RunTask : ProgressTask
    {
        private readonly Task Task;

        public RunTask(string title, Task task) : base(title)
        {
            Indeterminate = true;
            Task = task;
        }

        public override Task CreateTask()
        {
            return Task;
        }
    }
}
