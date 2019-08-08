using LibHac.Fs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                Source.CopyFileSystem(Destination, this);
            });
        }
    }
}
