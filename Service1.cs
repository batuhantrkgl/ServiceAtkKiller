using System.ServiceProcess;
using System.IO;
using System.Diagnostics;
using System.Management;
using System;

namespace ServiceAtkKiller
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Hizmet başladığında çalışacak kod buraya gelir
            StartUSBWatcher();
        }

        protected override void OnStop()
        {
            // Hizmet durduğunda çalışacak kod buraya gelir
        }

        private void StartUSBWatcher()
        {
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
            ManagementEventWatcher watcher = new ManagementEventWatcher(query);
            watcher.EventArrived += USBInserted;
            watcher.Start();
        }

        private void USBInserted(object sender, EventArrivedEventArgs e)
        {
            string usbDrive = FindUSBDrive();
            if (!string.IsNullOrEmpty(usbDrive))
            {
                string killTxtPath = Path.Combine(usbDrive, "kill.txt");
                if (File.Exists(killTxtPath))
                {
                    EventLog.WriteEntry("USB Watcher Hizmeti", "Kill USBsi takıldı");
                    RunTaskKill("FatihProjesi.exe");
                }
            }
        }

        private string FindUSBDrive()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
            {
                if (drive.DriveType == DriveType.Removable)
                {
                    return drive.RootDirectory.FullName;
                }
            }
            return null;
        }

        private void RunTaskKill(string processName)
        {
            try
            {
                Process taskkillProcess = new Process();
                taskkillProcess.StartInfo.FileName = "taskkill";
                taskkillProcess.StartInfo.Arguments = $"/F /IM {processName}";
                taskkillProcess.StartInfo.UseShellExecute = false;
                taskkillProcess.StartInfo.CreateNoWindow = true;
                taskkillProcess.StartInfo.RedirectStandardOutput = true;
                taskkillProcess.StartInfo.RedirectStandardError = true;

                taskkillProcess.Start();
                taskkillProcess.WaitForExit();

                EventLog.WriteEntry("USB Watcher Hizmeti", $"{processName} başarıyla sonlandırıldı.");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("USB Watcher Hizmeti", $"Hata: {ex.Message}");
            }
        }
    }
}
