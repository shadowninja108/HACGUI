using HACGUI.Extensions;
using HACGUI.Utilities;
using libusbK;
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
    // Payload injecting based on TegraSharp (aka copied and pasted)
    // https://github.com/simontime/TegraSharp

    public class InjectService
    {
        public static UsbDeviceInfo Device;

        private static readonly byte[] Intermezzo =
{
            0x44, 0x00, 0x9F, 0xE5, // LDR   R0, [PC, #0x44]
            0x01, 0x11, 0xA0, 0xE3, // MOV   R1, #0x40000000
            0x40, 0x20, 0x9F, 0xE5, // LDR   R2, [PC, #0x40]
            0x00, 0x20, 0x42, 0xE0, // SUB   R2, R2, R0
            0x08, 0x00, 0x00, 0xEB, // BL    #0x28
            0x01, 0x01, 0xA0, 0xE3, // MOV   R0, #0x40000000
            0x10, 0xFF, 0x2F, 0xE1, // BX    R0
            0x00, 0x00, 0xA0, 0xE1, // MOV   R0, R0
            0x2C, 0x00, 0x9F, 0xE5, // LDR   R0, [PC, #0x2C]
            0x2C, 0x10, 0x9F, 0xE5, // LDR   R1, [PC, #0x2C]
            0x02, 0x28, 0xA0, 0xE3, // MOV   R2, #0x20000
            0x01, 0x00, 0x00, 0xEB, // BL    #0xC
            0x20, 0x00, 0x9F, 0xE5, // LDR   R0, [PC, #0x20]
            0x10, 0xFF, 0x2F, 0xE1, // BX    R0
            0x04, 0x30, 0x90, 0xE4, // LDR   R3, [R0], #4
            0x04, 0x30, 0x81, 0xE4, // STR	 R3, [R1], #4
            0x04, 0x20, 0x52, 0xE2, // SUBS	 R2, R2, #4
            0xFB, 0xFF, 0xFF, 0x1A, // BNE	 #0xFFFFFFF4
            0x1E, 0xFF, 0x2F, 0xE1, // BX	 LR
            0x20, 0xF0, 0x01, 0x40, // ANDMI PC, R1, R0, LSR #32
            0x5C, 0xF0, 0x01, 0x40, // ANDMI PC, R1, IP, ASR R0
            0x00, 0x00, 0x02, 0x40, // ANDMI R0, R2, R0
            0x00, 0x00, 0x01, 0x40  // ANDMI R0, R1, R0
        };

        public static bool LibusbKInstalled => Device?.Service != null;

        private static readonly string VID = "0955";
        private static readonly string PID = "7321";
        private static string InstallString => $"libusbk,APX,{VID},{PID},{Guid.NewGuid()}";

        private static readonly ManagementEventWatcher CreateWatcher, DeleteWatcher;

        private static bool Started = false;
        private static int Writes;

        public static event Action DeviceInserted, DeviceRemoved;

        static InjectService()
        {
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
                StatusService.RCMStatus = StatusService.Status.OK;
                if (!LibusbKInstalled)
                {
                    MessageBoxResult result = MessageBox.Show("You have plugged in your console, but it lacks the libusbK driver. Want to install it? (You cannot inject anything until this is done)", "", MessageBoxButton.YesNo);
                    if(result == MessageBoxResult.Yes)
                        Install();
                }
            };

            DeviceRemoved += () =>
            {
                StatusService.RCMStatus = StatusService.Status.Incorrect;
            };
        }

        public static void Start()
        {
            if (Started)
                throw new Exception("Inject service is already started!");

            FindConsole();
            if (Device != null)
                Task.Run(() => DeviceInserted?.Invoke());

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

        private static byte[] SwizzlePayload(byte[] payload)
        {
            var buf = new byte[(int)Math.Ceiling((66216m + payload.Length) / 0x1000) * 0x1000];
            using (var mem = new MemoryStream())
            using (var wrt = new BinaryWriter(mem))
            {
                wrt.Write(0x30298);
                mem.Position = 0x2a8;
                for (var i = 0; i < 0x3c00; i++)
                    wrt.Write(0x4001f000);
                mem.Position = 0xf2a8;
                wrt.Write(Intermezzo);
                mem.Position = 0x102a8;
                wrt.Write(payload);
                Array.Copy(mem.ToArray(), buf, mem.Length);
                return buf;
            }
        }

        private static void WriteToUsb(UsbK wrt, byte[] payload)
        {
            var buffer = new byte[0x1000];

            for (var i = 0; i < payload.Length - 1; i += 0x1000, Writes++)
            {
                Buffer.BlockCopy(payload, i, buffer, 0, 0x1000);
                wrt.WritePipe(1, buffer, 0x1000, out _, IntPtr.Zero);
            }
        }

        public static void SendPayload(FileInfo info)
        {
            var patternMatch = new KLST_PATTERN_MATCH { ClassGUID = Device.ClassGuid };
            var deviceList = new LstK(0, ref patternMatch);
            deviceList.MoveNext(out KLST_DEVINFO_HANDLE deviceInfo);

            UsbK deviceUsb =  new UsbK(deviceInfo);
            deviceUsb.SetAltInterface(0, false, 0);

            byte[] payload = File.ReadAllBytes(info.FullName);
            var buf = new byte[0x10];
            deviceUsb.ReadPipe(0x81, buf, 0x10, out _, IntPtr.Zero);
            WriteToUsb(deviceUsb, SwizzlePayload(payload));

            if (Writes % 2 != 1)
            {
                Console.WriteLine("Switching buffers...");
                deviceUsb.WritePipe(1, new byte[0x1000], 0x1000, out _, IntPtr.Zero);
            }

            var setup = new WINUSB_SETUP_PACKET
            {
                RequestType = 0x81,
                Request = 0,
                Value = 0,
                Index = 0,
                Length = 0x7000
            };

            var result = deviceUsb.ControlTransfer(setup, new byte[0x7000], 0x7000, out var b, IntPtr.Zero);

            if (!result)
                MessageBox.Show($"Your switch doesn't appear to be vulnerable. Stack smash: 0x{b:x}");
        }

        public static void FindConsole()
        {
            Device = null;
            foreach (UsbDeviceInfo info in CreateUsbControllerDeviceInfos(GetUsbDevices()))
                if (info.DeviceID.StartsWith($"USB\\VID_{VID}&PID_{PID}"))
                {
                    Device = info;
                    break;
                }
        }

        public static void Install()
        {
            DirectoryInfo workingDirectory = HACGUIKeyset.ApxInstallerFolderInfo;
            FileInfo catSignerFile = workingDirectory.GetFile("dpscat.exe");
            LaunchProgram(
                catSignerFile.FullName,
                () => { },
                asAdmin: true,
                workingDirectory: workingDirectory.FullName);

            if (!workingDirectory.GetFile("nx.cat").Exists)
            {
                MessageBox.Show("Failed to sign driver.");
                return;
            }

            string fileName = "dpinst";
            if (Environment.Is64BitOperatingSystem)
                fileName += "64";
            else
                fileName += "32";
            fileName += ".exe";

            LaunchProgram(
                workingDirectory.GetFile(fileName).FullName,
                () => { },
                asAdmin: true);
        }
    }
}
