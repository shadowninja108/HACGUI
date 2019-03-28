using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class NullTask : ProgressTask
    {
        public NullTask(string title = "Nothing") : base(title)
        {

        }

        public override Task CreateTask()
        {
            return Task.CompletedTask;
        }
    }
}
