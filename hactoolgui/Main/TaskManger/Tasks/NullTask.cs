using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManger.Tasks
{
    public class NullTask : ProgressTask
    {
        public NullTask() : base("Nothing")
        {

        }

        public override Task StartAsync()
        {
            return Task.CompletedTask;
        }
    }
}
