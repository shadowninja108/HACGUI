using HACGUI.Utilities;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static HACGUI.Utilities.Native;

namespace HACGUI.Services
{
    public class InjectService
    {
        public static UsbDeviceInfo Device;

        public static bool LibusbKInstalled => Device?.Service != null;

        private static readonly string VID = "0955";
        private static readonly string PID = "7321";
        private static string InstallString => $"libusbk,APX,{VID},{PID},{Guid.NewGuid()}";

        private static readonly ManagementEventWatcher CreateWatcher, DeleteWatcher;

        private static bool Started = false;

        public static event Action DeviceInserted, DeviceRemoved;

        static InjectService()
        {
            if (HACGUIKeyset.TempQmkDevicesFileInfo.Exists)
                HACGUIKeyset.TempQmkDevicesFileInfo.Delete();
            if (HACGUIKeyset.TempQmkExecutableFileInfo.Exists)
                HACGUIKeyset.TempQmkExecutableFileInfo.Delete();

            // Create event handlers to detect when a device is added or removed
            CreateWatcher = new ManagementEventWatcher();
            WqlEventQuery createQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            CreateWatcher.EventArrived += new EventArrivedEventHandler((s, e) =>
            {
                FindConsole();
                if (Device != null)
                    DeviceInserted?.Invoke();
            });
            CreateWatcher.Query = createQuery;

            DeleteWatcher = new ManagementEventWatcher();
            WqlEventQuery deleteQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_PnPEntity'");
            DeleteWatcher.EventArrived += new EventArrivedEventHandler((s, e) =>
            {
                Device = null;
                FindConsole();
                if (Device == null)
                    DeviceRemoved?.Invoke();
            });
            DeleteWatcher.Query = deleteQuery;

            DeviceInserted += () =>
            {
                if (!LibusbKInstalled)
                {
                    MessageBoxResult result = MessageBox.Show("You have plugged in your console, but it lacks the libusbK driver. Want to install it? (You cannot inject anything until this is done)", "", MessageBoxButton.YesNo);
                    if(result == MessageBoxResult.Yes)
                    {
                        Install();
                    }
                }
            };
        }

        public static void Start()
        {
            if (Started)
                throw new Exception("Inject service is already started!");

            CreateWatcher.Start();
            DeleteWatcher.Start();

            Started = true;
        }

        public static void Stop()
        {
            if (!Started)
                throw new Exception("NAND service hasn't started yet!");

            CreateWatcher.Stop();
            DeleteWatcher.Stop();

            Started = false;
        }

        public static void FindConsole()
        {
            foreach (UsbDeviceInfo info in CreateUsbControllerDeviceInfos(GetUsbDevices()))
                if (info.DeviceID.StartsWith($"USB\\VID_{VID}&PID_{PID}"))
                {
                    Device = info;
                    break;
                }
        }

        public static void Install()
        {
            GitHubClient gitClient = new GitHubClient(new ProductHeaderValue("Github"));
            if(!HACGUIKeyset.TempQmkExecutableFileInfo.Exists)
                gitClient.Repository.Release.GetAll("qmk", "qmk_driver_installer")
                    .ContinueWith(task =>
                    {
                        IReadOnlyList<Release> releases = task.Result;
                        Release release = releases.FirstOrDefault();
                        return release.Assets.FirstOrDefault(r => r.BrowserDownloadUrl.EndsWith(".exe"));
                    }).ContinueWith(task => 
                    {
                        ReleaseAsset asset = task.Result;
                        HttpClient httpClient = new HttpClient();
                        httpClient.GetStreamAsync(asset.BrowserDownloadUrl).ContinueWith(streamTask =>
                        {
                            Stream src = streamTask.Result;
                            using (Stream dest = HACGUIKeyset.TempQmkExecutableFileInfo.OpenWrite())
                                src.CopyTo(dest);
                        }).Wait();
                    }).Wait();
            if (!HACGUIKeyset.TempQmkDevicesFileInfo.Exists)
            {
                byte[] text = Encoding.ASCII.GetBytes(InstallString);
                using (FileStream writer = HACGUIKeyset.TempQmkDevicesFileInfo.OpenWrite())
                    writer.Write(text, 0, text.Length);
            }

            LaunchProgram(
                HACGUIKeyset.TempQmkExecutableFileInfo.FullName, () =>
                {
                    ;
                },
                $"--force \"{HACGUIKeyset.TempQmkDevicesFileInfo.FullName}\"",
                false);
        }
    }
}
