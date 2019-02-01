using LibHac;
using System.Threading.Tasks;

namespace HACGUI.Main.Tasks
{
    public abstract class ProgressTask : IProgressReport
    {
        public delegate void MessageLoggedEvent(string message);
        public delegate void LongValueChangedEvent(long value);
        public event MessageLoggedEvent MessageLogged;
        public event LongValueChangedEvent ProgressChanged;
        public event LongValueChangedEvent TotalChanged;

        public string Title { get; internal set; }
        public long Progress { get; internal set; }
        public long Total { get; internal set; }

        public abstract Task StartAsync();

        public ProgressTask(string title)
        {
            Title = title;
        }

        public void LogMessage(string message)
        {
            MessageLogged(message + "\n");
        }

        public void Report(long value)
        {
            Progress = value;
            ProgressChanged(Progress);
        }

        public void ReportAdd(long value)
        {
            Progress += value;
            ProgressChanged(Progress);
        }

        public void SetTotal(long value)
        {
            Total = value;
            TotalChanged(value);
        }
    }
}
