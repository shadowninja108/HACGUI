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
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 or EventType = 3");

            Watcher.EventArrived += new EventArrivedEventHandler((s, e) =>
            {
                string driveName = e.NewEvent.Properties["DriveName"].Value.ToString();
                DriveInfo actedDrive = new DriveInfo(driveName);
                DirectoryInfo actedDriveInfo = actedDrive.RootDirectory;

                switch (e.NewEvent.Properties["EventType"].Value.ToString()) // cast to string because reading it as an int caused issues?
                {
                    case "2": // Drive plugged in
                        if (actedDrive.IsReady) // Not ready == not mountable, so ignored
                            if (Validator(actedDriveInfo))
                            {
                                CurrentDrive = actedDrive; // set current drive so the event handler *could* access it directly, but why tho
                                OnSDPluggedIn(actedDrive);
                            }
                        break;
                    case "3": // Drive removed
                        if (CurrentDrive != null) // if a drive hasn't been found to begin with, no need to check what was removed
                        {
                            DirectoryInfo currentDrive = new DirectoryInfo(CurrentDrive.Name);
                            if (currentDrive.Name == actedDrive.Name) // was the removed drive the one we found?
                            {
                                OnSDRemoved(CurrentDrive); // Allow the handler read the current drive before we clear it
                                CurrentDrive = null;
                            }
                        }
                        break;
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
            foreach (Delegate d in OnSDPluggedIn.GetInvocationList())
                OnSDPluggedIn -= (SDChangedEventHandler)d;
            foreach (Delegate d in OnSDRemoved.GetInvocationList())
                OnSDRemoved -= (SDChangedEventHandler)d;

            // Reset validator to default
            Validator = DefaultValidator;
        }
    }
}
