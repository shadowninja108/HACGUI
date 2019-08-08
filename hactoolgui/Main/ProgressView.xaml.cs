using HACGUI.Main.TaskManager.Tasks;
using LibHac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HACGUI.Main
{
    /// <summary>
    /// Interaction logic for ProgressView.xaml
    /// </summary>
    public partial class ProgressView : Page
    {
        private List<ProgressTask> Loggers;

        public long Value { get; private set; }
        private int CompletedLogs = 0;

        public ProgressView(List<ProgressTask> loggers)
        {
            InitializeComponent();

            Loggers = loggers;

            bool foundRunningTask = false;

            foreach (ProgressTask logger in Loggers)
            {
                logger.Started += () =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        string fullLog = logger.Log;
                        fullLog = fullLog ?? "\n";
                        fullLog = fullLog.Substring(0, fullLog.Length - 1);
                        string[] log = fullLog.Split('\n');
                        if (log != null && log.Length > 0)
                            Status.Content = log.Reverse().Last();
                        Log.Text += logger.Log;
                    }));
                };

                if(!foundRunningTask && logger.UnderlyingTask != null && logger.UnderlyingTask?.Status != TaskStatus.WaitingToRun)
                {
                    foundRunningTask = true;
                    logger.InformStart();
                }

                logger.MessageLogged += m =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        int caret = Log.CaretIndex;
                        int textLength = Log.Text.Length;

                        Status.Content = m;
                        Log.Text += m;

                        if (caret == textLength)
                        {
                            Log.ScrollToEnd();
                            Log.CaretIndex = textLength;
                        } else
                            Log.CaretIndex = caret;
                    }));

                };
                void handler(long v) => CalcProgress();

                logger.ProgressChanged += handler;
                logger.TotalChanged += handler;
                logger.Ended += () => 
                {
                    CompletedLogs++;
                    if (CompletedLogs >= Loggers.Count)
                    {
                        Window w = Parent as Window;
                        w.Dispatcher.Invoke(() => w.Close());
                    }
                };
            }
        }

        private void CalcProgress()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                long value = 0;
                long total = 0;

                foreach (ProgressTask logger in Loggers)
                {
                    value += logger.Progress;
                    total += logger.Total;
                    if (logger.Indeterminate)
                    {
                        ProgressBar.IsIndeterminate = true;
                        return;
                    }
                }

                if (total > 0)
                    ProgressBar.Value = (double)value / total;
                else
                    ProgressBar.Value = 1;
                ProgressBar.Maximum = 1;
            }));

        }
    }
}
