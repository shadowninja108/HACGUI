using LibHac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class NspPackTask : ProgressTask
    {
        private readonly Pfs0Builder Builder;
        private readonly FileInfo Target;

        public NspPackTask(Pfs0Builder builder, FileInfo target) : base($"Packing {target.Name}...")
        {
            Builder = builder;
            Target = target;
        }

        public override Task CreateTask()
        {
            return new Task(() => {
                Stream target = Target.Create();
                Builder.Build(target, this);
                target.Close();
            });
        }
    }
}
