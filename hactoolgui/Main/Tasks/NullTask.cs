using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.Tasks
{
    public class NullTask : ProgressTask
    {
        public override Task StartAsync()
        {
            return Task.CompletedTask;
        }
    }
}
