using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    // On god?
    public class RunTaskTask : ProgressTask
    {
        private Task Task;

        public RunTaskTask(string title, Task task) : base(title)
        {
            Indeterminate = true;
            Task = task;
        }

        public override Task StartAsync()
        {
            return Task;
        }
    }
}
