using HACGUI.Extensions;
using HACGUI.Main.TaskManager.Tasks;
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

namespace HACGUI.Main.TaskManager
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
                task.ProgressChanged += (v) =>
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        GetTaskElement(task).Binding?.UpdateTarget();
                    }));
                };
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

        private void GetProgressBarBinding(object sender, RoutedEventArgs e)
        {
            ProgressBar bar = sender as ProgressBar;
            (bar?.Tag as TaskElement).Binding = bar?.GetBindingExpression(ProgressBar.ValueProperty);
        }
    }
}
