using HACGUI.Main.TaskManager.Tasks;
using System.Windows.Data;

namespace HACGUI.Main.TaskManager
{
    public class TaskElement
    {
        public ProgressTask Task;

        public string Label => Task.Title;

        public BindingExpression Binding;

        public double Progress
        {
            get
            {
                double val = Task.Progress / (double)Task.Total;
                if (double.IsInfinity(val))
                    val = 1;
                if (double.IsNaN(val))
                    val = 0;
                return val;
            } 
        }

        public bool Indeterminate => Task.Indeterminate & Task.HasStarted;

        public TaskElement(ProgressTask task)
        {
            Task = task;
        }
    }
}
