using LibHac.IO;
using System.IO;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class NspPackTask : ProgressTask
    {
        private readonly PartitionFileSystemBuilder Builder;
        private readonly FileInfo Target;

        public NspPackTask(PartitionFileSystemBuilder builder, FileInfo target) : base($"Packing {target.Name}...")
        {
            Builder = builder;
            Target = target;
        }

        public override Task CreateTask()
        {
            return new Task(() => {
                Stream target = Target.Create();
                IStorage source = Builder.Build(PartitionFileSystemType.Standard);
                source.CopyToStream(target, source.GetSize(), this);
                target.Close();
            });
        }
    }
}
