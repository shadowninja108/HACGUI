using HACGUI.Extensions;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Services;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Navigation;

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
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    List.Items.Add(new TaskElement(task));
                }));
            };

            Queue.TaskStarted += (task) =>
            {
                StatusService.CurrentTask = task.Title;
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
                StatusService.CurrentTask = "";
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
            (bar?.Tag as TaskElement).Binding = bar?.GetBindingExpression(RangeBase.ValueProperty);
        }

        private void ViewButtonClicked(object sender, RoutedEventArgs e)
        {
            List<ProgressTask> tasks = new List<ProgressTask>();
            foreach(TaskElement element in List.SelectedItems)
                tasks.Add(element.Task);

            ProgressView view = new ProgressView(tasks);
            NavigationWindow window = new NavigationWindow
            {
                ShowsNavigationUI = false, // get rid of the t r a s h
                Owner = Window.GetWindow(this)
            };

            window.Navigate(view);
            window.ShowDialog();
        }
    }
}
