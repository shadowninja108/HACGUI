using HACGUI.Main.TaskManager.Tasks;
using LibHac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HACGUI.Main
{
    /// <summary>
    /// Interaction logic for ProgressView.xaml
    /// </summary>
    public partial class ProgressView : Page
    {
        private List<ProgressTask> Loggers;

        public long Value { get; private set; }

        public ProgressView(List<ProgressTask> loggers)
        {
            InitializeComponent();

            Loggers = loggers;

            foreach(ProgressTask logger in Loggers)
            {
                logger.MessageLogged += m =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Status.Content = m;
                        Log.Text += m;
                    }));

                };
                void handler(long v) => CalcProgress();

                logger.ProgressChanged += handler;
                logger.TotalChanged += handler;
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
                }

                if (total > 0)
                    ProgressBar.Value = (double)value / total;
                else
                    ProgressBar.Value = 1;
                ProgressBar.Maximum = 1;

                if (value >= total)
                    (Parent as Window).Close();
            }));

        }
    }
}
