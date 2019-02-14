using HACGUI.Extensions;
using HACGUI.Main.TaskManger.Tasks;
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

namespace HACGUI.Main.TaskManger
{
    /// <summary>
    /// Interaction logic for TaskManager.xaml
    /// </summary>
    public partial class TaskManagerPage : PageExtension
    {
        public static TaskManagerPage Current;

        public TaskQueue Queue;

        public TaskManagerPage()
        {
            InitializeComponent();
            Current = this;
            Queue = new TaskQueue();


            Queue.TaskQueued += (task) =>
            {
                List.Items.Add(new TaskElement(task));
                
            };

            Queue.TaskStarted += (task) =>
            {
                /*TaskElement element = GetTaskElement(task);
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    ProgressBar bar = new ProgressBar
                    {
                        Minimum = 0,
                        Maximum = 1
                    };

                    void handler(long v)
                    {
                        bar.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            bar.Value = (double)task.Progress / task.Total;
                        }));
                    }
                    task.ProgressChanged += handler;
                    task.TotalChanged += handler;
                }));*/
            };

            Queue.TaskCompleted += (task) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    List.Items.Remove(GetTaskElement(task));
                }));
            };
        }

        public TaskElement GetTaskElement(ProgressTask task)
        {
            foreach (TaskElement element in List.Items)
                if (element.Task == task)
                    return element;
            return null;
        }
    }
}
