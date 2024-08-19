using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace APKInstaller
{
    class Program
    {
        static Label deviceInfoLabel;
        static Label apkFileLabel;
        static string apkFilePath;
        static ProgressBar progressBar;
        static Label statusLabel;
        static Label adbStatusLabel;
        static Label deviceStatusLabel;
        static Button fileExplorerButton;
        static Button installButton;
        static Button checkStatusButton;

        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Console.WriteLine("Welcome to the APK Installer!");

            // Create the UI
            CreateUI();

            Application.Run();
        }

        static async void CreateUI()
        {
            Form form = new Form
            {
                Text = "APK Installer",
                Size = new Size(500, 300)
            };

            form.FormClosing += new FormClosingEventHandler(Form_FormClosing);

            // ADB Status Label
            adbStatusLabel = new Label
            {
                Location = new Point(10, 10),
                AutoSize = true,
                Text = "Checking ADB installation..."
            };
            form.Controls.Add(adbStatusLabel);

            // Device Status Label
            deviceStatusLabel = new Label
            {
                Location = new Point(10, 40),
                AutoSize = true,
                Text = "Checking device connectivity..."
            };
            form.Controls.Add(deviceStatusLabel);

            // Device Info Label
            deviceInfoLabel = new Label
            {
                Location = new Point(10, 70),
                AutoSize = true,
                Text = "Connected Device: None"
            };
            form.Controls.Add(deviceInfoLabel);

            // APK File Label
            apkFileLabel = new Label
            {
                Location = new Point(10, 130),
                AutoSize = true,
                Text = "Selected APK File: None"
            };
            form.Controls.Add(apkFileLabel);

            // File Explorer Button
            fileExplorerButton = new Button
            {
                Location = new Point(10, 160),
                Size = new Size(120, 30),
                Text = "Select APK File",
                Enabled = false
            };
            fileExplorerButton.Click += FileExplorerButton_Click;
            form.Controls.Add(fileExplorerButton);

            // Install Button
            installButton = new Button
            {
                Location = new Point(140, 160),
                Size = new Size(120, 30),
                Text = "Install APK",
                Enabled = false
            };
            installButton.Click += InstallButton_Click;
            form.Controls.Add(installButton);

            // Check Status Button
            checkStatusButton = new Button
            {
                Location = new Point(270, 160),
                Size = new Size(120, 30),
                Text = "Check Status"
            };
            checkStatusButton.Click += CheckStatusButton_Click;
            form.Controls.Add(checkStatusButton);

            // Progress Bar
            progressBar = new ProgressBar
            {
                Location = new Point(10, 200),
                Size = new Size(250, 20),
                Visible = false
            };
            form.Controls.Add(progressBar);

            // Status Label
            statusLabel = new Label
            {
                Location = new Point(10, 230),
                AutoSize = true,
                Text = ""
            };
            form.Controls.Add(statusLabel);

            form.Shown += async (sender, e) => await InitializeAsync();

            Application.Run(form);
        }

        static async Task InitializeAsync()
        {
            await CheckStatusAsync();
        }

        static async void CheckStatusButton_Click(object sender, EventArgs e)
        {
            await CheckStatusAsync();
        }

        static async Task CheckStatusAsync()
        {
            bool isAdbInstalled = await Task.Run(() => IsAdbInstalled("adb"));
            if (isAdbInstalled)
            {
                adbStatusLabel.Text = "ADB installation: ✔️";
                adbStatusLabel.ForeColor = Color.Green;
            }
            else
            {
                adbStatusLabel.Text = "ADB installation: ❌";
                adbStatusLabel.ForeColor = Color.Red;
            }

            string deviceInfo = await Task.Run(() => ConnectToDevice());
            if (!string.IsNullOrEmpty(deviceInfo))
            {
                deviceStatusLabel.Text = "Device connectivity: ✔️";
                deviceStatusLabel.ForeColor = Color.Green;
                deviceInfoLabel.Text = $"Connected Device: {deviceInfo}";
            }
            else
            {
                deviceStatusLabel.Text = "Device connectivity: ❌";
                deviceStatusLabel.ForeColor = Color.Red;
            }

            fileExplorerButton.Enabled = isAdbInstalled && !string.IsNullOrEmpty(deviceInfo);
            installButton.Enabled = isAdbInstalled && !string.IsNullOrEmpty(deviceInfo);
        }

        static bool IsAdbInstalled(string adbPath)
        {
            try
            {
                Process.Start(adbPath, "version").WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }

        static string ConnectToDevice()
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = "devices",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Extract connected device information
            string deviceInfo = string.Empty;
            if (!string.IsNullOrEmpty(output))
            {
                string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    // The connected device information is in the second line
                    deviceInfo = lines[1];
                }
            }

            return deviceInfo;
        }

        static void FileExplorerButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "APK Files (*.apk)|*.apk";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    apkFilePath = openFileDialog.FileName;
                    apkFileLabel.Text = $"Selected APK File: {Path.GetFileName(apkFilePath)}";
                    Console.WriteLine($"Selected APK File: {apkFilePath}");
                }
            }
        }

        static void InstallButton_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(apkFilePath))
            {
                MessageBox.Show("Please select an APK file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            statusLabel.Text = "Installing APK...";
            statusLabel.ForeColor = Color.Black;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
            worker.RunWorkerAsync(apkFilePath);
        }

        static void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string apkFilePath = (string)e.Argument;
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "adb",
                Arguments = $"install \"{apkFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                e.Result = true;
            }
            else
            {
                e.Result = false;
                e.Result += error;
            }
        }

        static void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visible = false;

            if ((bool)e.Result)
            {
                statusLabel.Text = "APK installed successfully! ✔️";
                statusLabel.ForeColor = Color.Green;
            }
            else
            {
                statusLabel.Text = "Failed to install APK ❌";
                statusLabel.ForeColor = Color.Red;
            }
        }

        static void Form_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Ensure all processes related to the application are killed
            Process.GetCurrentProcess().Kill();
        }
    }
}
