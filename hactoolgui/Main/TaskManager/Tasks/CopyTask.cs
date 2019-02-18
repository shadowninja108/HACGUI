using LibHac.IO;
using System;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class CopyTask : ProgressTask
    {
        private readonly IStorage Source, Destination;
        private readonly string Message;

        public CopyTask(IStorage source, IStorage destination, string message = "") : base(message)
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
