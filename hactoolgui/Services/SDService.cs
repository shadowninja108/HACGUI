using System;
using System.IO;
using System.Management;

namespace HACGUI.Services
{
    public class SDService
    {
        public delegate void SDChangedEventHandler(DriveInfo drive);

        public static Func<DirectoryInfo, bool> Validator = DefaultValidator;
        public static event SDChangedEventHandler OnSDPluggedIn, OnSDRemoved;
        public static DriveInfo CurrentDrive;

        private static ManagementEventWatcher Watcher;
        private static bool Started = false;

        private static Func<DirectoryInfo, bool> DefaultValidator = 
            (info) =>
            {
                // TODO: actually do it lol
                return false;
            };

        static SDService()
        {
            // Create an event handler to detect when a drive is added or removed
            Watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_LogicalDisk' AND TargetInstance.Description = \"Removable disk\"");

            Watcher.EventArrived += new EventArrivedEventHandler((s, e) =>
            {
                string driveName = ((ManagementBaseObject)e.NewEvent["TargetInstance"]).Properties["DeviceID"].Value.ToString();
                DriveInfo actedDrive = new DriveInfo(driveName);
                DirectoryInfo actedDriveInfo = actedDrive.RootDirectory;
                Console.WriteLine(e.NewEvent.ClassPath.ClassName);

                if (actedDrive.IsReady) {
                    if (Validator(actedDriveInfo)) {
                        CurrentDrive = actedDrive; // set current drive so the event handler *could* access it directly, but why tho
                        OnSDPluggedIn(actedDrive);
                    }
                } else {
                    if (CurrentDrive != null) // if a drive hasn't been found to begin with, no need to check what was removed
                        {
                        DirectoryInfo currentDrive = new DirectoryInfo(CurrentDrive.Name);
                        if (currentDrive.Name == actedDrive.Name) // was the removed drive the one we found?
                        {
                            OnSDRemoved(CurrentDrive); // Allow the handler read the current drive before we clear it
                            CurrentDrive = null;
                        }
                    }
                }
            });
            Watcher.Query = query;
        }

        public static void Start()
        {
            if (Started)
                throw new Exception("SD service is already started!");

            // Do initial scan of drives
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady)
                {
                    DirectoryInfo root = drive.RootDirectory;
                    if (Validator(root))
                    {
                        CurrentDrive = drive;
                        OnSDPluggedIn(drive);
                    }
                }
            }

            Watcher.Start();

            Started = true;
        }

        public static void Stop()
        {
            if (!Started)
                throw new Exception("SD service hasn't started yet!");

            Watcher.Stop();

            Started = false;
        }

        public static void ResetHandlers()
        {
            // Clear event handlers
            if(OnSDPluggedIn != null)
                foreach (Delegate d in OnSDPluggedIn.GetInvocationList())
                    OnSDPluggedIn -= (SDChangedEventHandler)d;
            if (OnSDRemoved != null)
                foreach (Delegate d in OnSDRemoved.GetInvocationList())
                    OnSDRemoved -= (SDChangedEventHandler)d;

            // Reset validator to default
            Validator = DefaultValidator;
        }
    }
}
