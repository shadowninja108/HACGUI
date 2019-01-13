using LibHac.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.Tasks
{
    public class CopyTask : ProgressTask
    {
        private readonly IStorage Source, Destination;
        private readonly string Message;

        public CopyTask(IStorage source, IStorage destination, string message = "") : base()
        {
            Source = source;
            Destination = destination;
            Message = message;
        }

        public override Task StartAsync()
        {
            return new Task(() => 
            {
                LogMessage(Message);
                Source.CopyToStream(Destination.AsStream(), Source.Length, this);
                Source.Dispose();
                Destination.Dispose();
            });
        }
    }
}
