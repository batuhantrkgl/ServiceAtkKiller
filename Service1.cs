using System.ServiceProcess;
using System.IO;
using System.Diagnostics;
using System.Management;
using System;
using System.ComponentModel;

namespace ServiceAtkKiller
{
    public partial class USBWatcherSerivceOne : ServiceBase
    {
        public USBWatcherSerivceOne()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Ensure 64-bit execution
            #if !DEBUG
            SetProcessTo64Bit();
            #endif

            StartUSBWatcher();
            string workingDirectory = Directory.GetCurrentDirectory();
            EventLog.WriteEntry("Current working directory: " + workingDirectory);
        }

        protected override void OnStop()
        {
            // Hizmet durduğunda çalışacak kod buraya gelir
        }

        private void SetProcessTo64Bit()
        {
            if (IntPtr.Size == 8) // If running on 64-bit system
            {
                Process currentProcess = Process.GetCurrentProcess();
                currentProcess.StartInfo.UseShellExecute = false;
                currentProcess.StartInfo.FileName = "cmd.exe";
                currentProcess.StartInfo.Arguments = "/C corflags \"" + currentProcess.MainModule.FileName + "\" /32BIT-";

                currentProcess.Start();
                currentProcess.WaitForExit();
            }
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
                    EventLog.WriteEntry("USB Watcher Hizmeti - USBInstered / @Batuhantrkgl", "kill.txt USBsi takıldı. Servis Çalıştırılıyor..");
                    RunTaskKill("ConfigServices.exe");
                    RunTaskKill("FatihProjesi.exe");
                    RunExe();
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

                EventLog.WriteEntry("USB Watcher Hizmeti - RunTaskkill() / @Batuhantrkgl", $"{processName} başarıyla sonlandırıldı.");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("USB Watcher Hizmeti - RunTaskkill() / @Batuhantrkgl", $"Hata: {ex.Message}");
            }
        }
        private void RunExe()
        {
            try
            {
                string usbDrive = FindUSBDrive();
                string startBatPath = Path.Combine(usbDrive, "start.bat");
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                if (File.Exists(startBatPath) == false)
                {
                    EventLog.WriteEntry("USB Watcher Hizmeti - RunExe() / @Batuhantrkgl", "ERROR: start.bat file not found.");
                    return;
                }
                else
                {
                    EventLog.WriteEntry("USB Watcher Hizmeti - RunExe() / @Batuhantrkgl", $"Executing: {startBatPath}");

                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(startBatPath);
                    process.StartInfo.FileName = "cmd.exe";
                    process.StartInfo.Arguments = $"/C \"{startBatPath}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    process.WaitForExit();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    int exitCode = process.ExitCode;

                    if (exitCode == 0)
                    {
                        EventLog.WriteEntry("USB Watcher Hizmeti - RunExe() / @Batuhantrkgl", $"{startBatPath} successfully executed.");
                    }
                    else
                    {
                        EventLog.WriteEntry("USB Watcher Hizmeti - RunExe() / @Batuhantrkgl", $"{startBatPath} execution failed with exit code {exitCode}.\nOutput: {output}\nError: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("USB Watcher Hizmeti - RunExe() / @Batuhantrkgl", $"Hata: {ex.Message}");
            }
        }
    }
}
