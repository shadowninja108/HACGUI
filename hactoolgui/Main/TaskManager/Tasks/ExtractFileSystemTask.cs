using LibHac.Fs;
using LibHac.FsSystem;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class ExtractFileSystemTask : ProgressTask
    {
        private IFileSystem Source, Destination;

        public ExtractFileSystemTask(string title, IFileSystem source, IFileSystem destination) : base(title)
        {
            Source = source;
            Destination = destination;
        }
        public ExtractFileSystemTask(string title, IFileSystem source, string destination) : this(title, source, new LocalFileSystem(destination))
        {
        }

        public override Task CreateTask()
        {
            return new Task(() => 
            {
                foreach(DirectoryEntryEx entry in Source.EnumerateEntries("*", SearchOptions.RecurseSubdirectories))
                {
                    if(entry.Type == DirectoryEntryType.Directory)
                        Source.CopyDirectory(Destination, entry.FullPath, entry.FullPath, this);
                }
            });
        }
    }
}
