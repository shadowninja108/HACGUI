using HACGUI.Main.TaskManger.Tasks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace HACGUI.Main.TaskManger
{
    public class TaskElement
    {
        public ProgressTask Task;

        public string Label => Task.Title;

        public ProgressBar Bar { get; set; }

        public double Progress { get; set; }

        public TaskElement(ProgressTask task)
        {
            Task = task;

            task.ProgressChanged += (v) =>
            {
                double val = Task.Progress / (double)Task.Total;
                if (double.IsInfinity(val))
                    val = 1;
                if (double.IsNaN(val))
                    val = 0;
                Progress = val;
                
            };
        }
    }
}
