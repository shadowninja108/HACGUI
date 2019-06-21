using HACGUI.Extensions;
using HACGUI.Utilities;
using LibHac;
using System.IO;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class SaveKeysetTask : ProgressTask
    {
        private readonly string ConsoleName;

        public SaveKeysetTask(string consoleName) : base("Saving keyset...")
        {
            Indeterminate = true;
            Blocking = true;
            ConsoleName = consoleName;
        }

        public override Task CreateTask()
        {
            return new Task(() => 
            {
                Stream prodKeys = HACGUIKeyset.ProductionKeysFileInfo.Create();
                prodKeys.WriteString(HACGUIKeyset.PrintCommonKeys(HACGUIKeyset.Keyset, true));
                Stream extraKeys = HACGUIKeyset.ExtraKeysFileInfo.Create();
                extraKeys.WriteString(HACGUIKeyset.PrintCommonWithoutFriendlyKeys(HACGUIKeyset.Keyset));
                Stream consoleKeys = HACGUIKeyset.ConsoleKeysFileInfo.Create();
                consoleKeys.WriteString(ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset));
                if (ConsoleName != null)
                {
                    Stream specificConsoleKeys = HACGUIKeyset.GetConsoleKeysFileInfoByName(ConsoleName).Create();
                    specificConsoleKeys.WriteString(ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset));
                    specificConsoleKeys.Close();
                }
                Stream titleKeys = HACGUIKeyset.TitleKeysFileInfo.Create();
                titleKeys.WriteString(ExternalKeys.PrintTitleKeys(HACGUIKeyset.Keyset));
                prodKeys.Close();
                extraKeys.Close();
                consoleKeys.Close();
                titleKeys.Close();
            });
        }
    }
}
