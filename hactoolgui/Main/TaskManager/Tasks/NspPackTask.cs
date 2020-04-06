using LibHac.Fs;
using LibHac.FsSystem;
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
                IStorage source = Builder.Build(PartitionFileSystemType.Standard);
                using (Stream target = Target.Create()) {
                    source.GetSize(out long size);
                    source.CopyToStream(target, size, this);
                }
            });
        }
    }
}
