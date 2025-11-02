using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace iLiberate_indev
{
    public partial class Form1 : Form
    {
        // Win32 calls to support dragging from client area. will maybe add soon idk
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        const int WM_NCLBUTTONDOWN = 0xA1;
        readonly IntPtr HTCAPTION = (IntPtr)0x2;

        private ManagementEventWatcher deviceConnectionWatcher;
        private bool devicePaired = false;
        private string idevicepairPath;
        private LogWindow logWindow;
        private bool isProcessingDevice = false;
        private System.Windows.Forms.Timer countdownTimer;
        private int countdownValue = 5;
        
        public Form1()
        {
            InitializeComponent(); 

            this.StartPosition = FormStartPosition.CenterScreen;

         
            var logo = new TransparentPictureBox
            {
                Image = Image.FromFile("iliberate.resources/logo.png"),
                Location = new Point(12, 12),
                Size = new Size(50, 50),
                BackColor = Color.Transparent,
                Parent = Header
            };
            Header.Controls.Add(logo);

          
            logWindow = new LogWindow();
            
           
            countdownTimer = new System.Windows.Forms.Timer();
            countdownTimer.Interval = 1000; 
            countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            MessageBox.Show("This tool is in-development. Consider it risky for use.");
            progress = new NotifyIcon
            {
                Icon = SystemIcons.Application, 
                Visible = true,
                Text = "iLiberate+indev 0.1"
            };

            progress.ShowBalloonTip(
            5000,
            "iLiberate+indev",
            "iLiberate+indev is running in the background.",
            ToolTipIcon.Info
            );

        
            InitializeDeviceMonitoring();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            
            if (keyData == (Keys.Control | Keys.Alt | Keys.L))
            {
                if (logWindow.Visible)
                {
                    logWindow.Hide();
                }
                else
                {
                    logWindow.Show();
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripStatusLabel1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {
            
        }

        private void Log(string message)
        {
            logWindow?.AddLog(message);
            Debug.WriteLine(message);
        }

        private void InitializeDeviceMonitoring()
        {
            Log("Initializing device monitoring...");
          
            FindIdevicepairExe();

            try
            {
              
                WqlEventQuery query = new WqlEventQuery(
                    "SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2");
                
                deviceConnectionWatcher = new ManagementEventWatcher(query);
                deviceConnectionWatcher.EventArrived += DeviceConnectionWatcher_EventArrived;
                deviceConnectionWatcher.Start();
                Log("Device monitoring started");
 
                CheckForConnectedDevices();
            }
            catch (Exception ex)
            {
                Log($"Failed to initialize device monitoring: {ex.Message}");
                MessageBox.Show($"Failed to initialize device monitoring: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FindIdevicepairExe()
        {
          
            string[] searchPaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "refs", "idevicepair.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build", "refs", "idevicepair.exe"),
                Path.Combine(Application.StartupPath, "refs", "idevicepair.exe"),
                Path.Combine(Application.StartupPath, "build", "refs", "idevicepair.exe")
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    idevicepairPath = path;
                    Log($"Found idevicepair.exe at: {path}");
                    return;
                }
            }
            
            Log("Warning: idevicepair.exe not found in common locations");
        }

        private void CheckForConnectedDevices()
        {
            try
            {
                Log("Scanning for connected Apple devices...");
                using (var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity"))
                {
                    foreach (ManagementObject device in searcher.Get())
                    {
                        string description = device["Description"]?.ToString() ?? "";
                        string manufacturer = device["Manufacturer"]?.ToString() ?? "";
                        string name = device["Name"]?.ToString() ?? "";
                        
                        string allInfo = (description + " " + manufacturer + " " + name).ToLower();
                        
                        if (allInfo.Contains("apple") || 
                            allInfo.Contains("iphone") ||
                            allInfo.Contains("ipad") ||
                            allInfo.Contains("ipod"))
                        {
                            Log($"Apple device detected: {description} - {manufacturer} - {name}");
                            HandleDeviceDetected();
                            break;
                        }
                    }
                }
                Log("Device scan completed");
            }
            catch (Exception ex)
            {
                Log($"Error checking for devices: {ex.Message}");
            }
        }

        private void DeviceConnectionWatcher_EventArrived(object sender, EventArrivedEventArgs e)
        {
            if (isProcessingDevice)
            {
                Log("USB device connection event detected (ignored - already processing)");
                return;
            }
            
            Log("USB device connection event detected");
        
            if (InvokeRequired)
            {
                Invoke(new Action(() => CheckForConnectedDevices()));
            }
            else
            {
                CheckForConnectedDevices();
            }
        }

        private void HandleDeviceDetected()
        {
            if (!devicePaired && !isProcessingDevice)
            {
                isProcessingDevice = true;
                devicePaired = true;
                Log("Processing device connection...");

           
                UpdateStatus("Device connected. Starting pairing...");
                label5.Text = "device connected";
                label1.Text = "Device connected. Starting pairing...";

              
                if (InvokeRequired)
                {
                    Invoke(new Action(() => {
                        toolStripProgressBar1.Visible = true;
                        toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                    }));
                }
                else
                {
                    toolStripProgressBar1.Visible = true;
                    toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
                }

                
                countdownValue = 5;
                countdownTimer.Start();
                UpdateCountdown();

                
                if (!string.IsNullOrEmpty(idevicepairPath))
                {
                    Log("Starting 5 second countdown before running idevicepair...");
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => {
                                countdownTimer.Stop();
                                toolStripTime2.Text = "";
                                UpdateStatus("Pairing...");
                            }));
                        }
                        Log($"Running idevicepair.exe from: {idevicepairPath}");
                        RunIdevicepair();
                    });
                }
                else
                {
                    Log("Warning: idevicepair.exe not found, enabling button anyway");
                   
                    UpdateStatus("Ready");
                    EnableNextButton();
                    isProcessingDevice = false;
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => toolStripProgressBar1.Visible = false));
                    }
                    else
                    {
                        toolStripProgressBar1.Visible = false;
                    }
                }
            }
        }

        private void RunIdevicepair()
        {
            Task.Run(() =>
            {
                try
                {
                    Log($"Executing: {idevicepairPath} pair");
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = idevicepairPath,
                        Arguments = "pair",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(startInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        Log($"idevicepair exit code: {process.ExitCode}");
                        if (!string.IsNullOrEmpty(output))
                            Log($"idevicepair output: {output}");
                        if (!string.IsNullOrEmpty(error))
                            Log($"idevicepair error: {error}");

                        
                        bool hasPasscode = output.Contains("This device is passcode protected") || 
                                         error.Contains("This device is passcode protected") ||
                                         output.Contains("passcode") ||
                                         error.Contains("passcode");

                        
                        bool pairSuccess = output.Contains("SUCCESS") || 
                                         output.Contains("Paired with") ||
                                         process.ExitCode == 0;

                        Log($"Pairing result: {(pairSuccess ? "SUCCESS" : "FAILED")}");

                        if (hasPasscode)
                        {
                            Log("Device has a passcode - showing unlock instructions");
                            string message = "Your device has a passcode. Please unlock your iPhone and the app will automatically reconnect.";
                            if (InvokeRequired)
                            {
                                Invoke(new Action(() => {
                                    label1.Text = message;
                                    MessageBox.Show(message, "Unlock Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    AfterPairingCompleted(false);
                                }));
                            }
                            else
                            {
                                label1.Text = message;
                                MessageBox.Show(message, "Unlock Required", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                AfterPairingCompleted(false);
                            }
                        }
                        else if (InvokeRequired)
                        {
                            Invoke(new Action(() => AfterPairingCompleted(pairSuccess)));
                        }
                        else
                        {
                            AfterPairingCompleted(pairSuccess);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error running idevicepair: {ex.Message}");
                    if (InvokeRequired)
                    {
                        Invoke(new Action(() => 
                        {
                            EnableNextButton();
                            isProcessingDevice = false;
                        }));
                    }
                    else
                    {
                        EnableNextButton();
                        isProcessingDevice = false;
                    }
                }
            });
        }

        private void UpdateStatus(string status)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => toolStripTime2.Text = status));
            }
            else
            {
                toolStripTime2.Text = status;
            }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            UpdateCountdown();
        }

        private void UpdateCountdown()
        {
            if (countdownValue > 0)
            {
                UpdateStatus($"Starting in {countdownValue}...");
                countdownValue--;
            }
            else
            {
                countdownTimer.Stop();
            }
        }

        private void AfterPairingCompleted(bool success)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => {
                    toolStripProgressBar1.Visible = false;
                    toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
                }));
            }
            else
            {
                toolStripProgressBar1.Visible = false;
                toolStripProgressBar1.Style = ProgressBarStyle.Blocks;
            }

            if (success)
            {
                label5.Text = "Device paired";
                label1.Text = "Device paired successfully. You can now continue.";
                UpdateStatus("Paired");
                Log("Device pairing completed successfully");
            }
            else
            {
                label5.Text = "Device connected (pairing failed)";
                label1.Text = "Device connected but pairing failed. You may still proceed.";
                UpdateStatus("Pairing failed");
                Log("Device pairing failed but button enabled anyway");
            }
            
            EnableNextButton();
            isProcessingDevice = false;
        }

        private void EnableNextButton()
        {
            Log("Enabling Next button");
            buttonNext1.Enabled = true;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up the watcher
            if (deviceConnectionWatcher != null)
            {
                deviceConnectionWatcher.Stop();
                deviceConnectionWatcher.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}
