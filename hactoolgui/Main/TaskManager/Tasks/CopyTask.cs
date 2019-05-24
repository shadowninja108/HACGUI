using LibHac.Fs;
using System;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class CopyTask : ProgressTask
    {
        private readonly IStorage Source, Destination;
        private readonly string Message;

        public CopyTask(string message, IStorage source, IStorage destination) : base(message)
        {
            Source = source;
            Destination = destination;
            Message = message;
        }

        public override Task CreateTask()
        {
            return new Task(() => 
            {
                LogMessage(Message);
                Source.CopyTo(Destination, this);
                Source.Dispose();
                Destination.Dispose();
            });
        }
    }
}
