using LibHac;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HACGUI.Utilities;

namespace HACGUI
{
    /// <summary>
    /// Interaction logic for ExceptionWindow.xaml
    /// </summary>
    public partial class ExceptionWindow : Window
    {
        public ExceptionWindow(Exception e)
        {
            InitializeComponent();
            if (RootWindow.Current != null) // cleanup
                RootWindow.Current.Close();

            TextBox.Text = e.ToString();

            Task.Factory.StartNew(() => {
                try
                {
                    using (FileStream zipOut = HACGUIKeyset.GetCrashZip().Create())
                    {
                        using (ZipArchive archive = new ZipArchive(zipOut, ZipArchiveMode.Update))
                        {
                            ZipArchiveEntry prodEntry = archive.CreateEntry("prod.keys");
                            using (StreamWriter writer = new StreamWriter(prodEntry.Open()))
                                writer.Write(HACGUIKeyset.PrintCommonKeys(HACGUIKeyset.Keyset, true));
                            ZipArchiveEntry extraEntry = archive.CreateEntry("extra.keys");
                            using (StreamWriter writer = new StreamWriter(extraEntry.Open()))
                                writer.Write(HACGUIKeyset.PrintCommonWithoutFriendlyKeys(HACGUIKeyset.Keyset));
                            ZipArchiveEntry consoleEntry = archive.CreateEntry("console.keys");
                            using (StreamWriter writer = new StreamWriter(consoleEntry.Open()))
                                writer.Write(ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset));
                            ZipArchiveEntry titleEntry = archive.CreateEntry("title.keys");
                            using (StreamWriter writer = new StreamWriter(titleEntry.Open()))
                                writer.Write(ExternalKeys.PrintTitleKeys(HACGUIKeyset.Keyset));
                            ZipArchiveEntry exceptionEntry = archive.CreateEntry("exception.txt");
                            using (StreamWriter writer = new StreamWriter(exceptionEntry.Open()))
                                writer.Write(e.ToString());
                        }
                    }
                }
                catch (Exception e2)
                {
                    TextBox.Dispatcher.BeginInvoke(new Action(() => 
                    {
                        TextBox.Text += "Crash packer has also encountered an error...\n";
                        TextBox.Text += e2.ToString();
                    }));

                }
            });   
        }
    }
}
