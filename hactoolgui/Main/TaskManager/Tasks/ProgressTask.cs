using LibHac;
using System.Threading;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public abstract class ProgressTask : IProgressReport
    {
        public delegate void MessageLoggedEvent(string message);
        public delegate void LongValueChangedEvent(long value);
        public delegate void VoidEvent();
        public event MessageLoggedEvent MessageLogged;
        public event LongValueChangedEvent ProgressChanged;
        public event LongValueChangedEvent TotalChanged;
        public event VoidEvent Started;

        public string Title { get; internal set; }
        public string Log { get; internal set; } = "";

        public long Progress { get; internal set; }
        public long Total { get; internal set; }

        public bool Indeterminate { get; internal set; } = false;
        public bool Blocking { get; internal set; } = true;
        public bool HasStarted { get; internal set; } = false;

        public Task UnderlyingTask { get; internal set; }

        public abstract Task CreateTask();

        public ProgressTask(string title)
        {
            Title = title;
            LogMessage(title);
        }

        public void LogMessage(string message)
        {
            Title = message;
            Log += message + "\n";
            MessageLogged?.Invoke(message + "\n");
        }

        public void Report(long value)
        {
            Progress = value;
            ProgressChanged?.Invoke(Progress);
        }

        public void ReportAdd(long value)
        {
            Progress += value;
            ProgressChanged?.Invoke(Progress);
        }

        public void SetTotal(long value)
        {
            Total = value;
            TotalChanged?.Invoke(value);
        }

        public void InformStart()
        {
            HasStarted = true;
            Started?.Invoke();
        }
    }
}
