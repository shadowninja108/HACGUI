using LibHac.Fs;
using LibHac.FsSystem;
using System;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class CopyTask : ProgressTask
    {
        private readonly IStorage Source, Destination;
        private readonly string Message;
        private readonly bool DisposeSource;

        public CopyTask(string message, IStorage source, IStorage destination, bool disposeSource = true) : base(message)
        {
            Source = source;
            Destination = destination;
            Message = message;
            DisposeSource = disposeSource;
        }

        public override Task CreateTask()
        {
            return new Task(() => 
            {
                LogMessage(Message);
                Source.CopyTo(Destination, this);
                if(DisposeSource)
                    Source.Dispose();
                Destination.Dispose();
            });
        }
    }
}
