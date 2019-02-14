using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class NullTask : ProgressTask
    {
        public NullTask(string title = "Nothing") : base(title)
        {

        }

        public override Task StartAsync()
        {
            return Task.CompletedTask;
        }
    }
}
