using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace VWiFiManager
{
    public partial class MainForm : Form
    {
        private uint m_previousExecutionState;

        public MainForm()
        {
            InitializeComponent();

            // Set new state to prevent system sleep (note: still allows screen saver)
            m_previousExecutionState = NativeMethods.SetThreadExecutionState(
                NativeMethods.ES_CONTINUOUS | NativeMethods.ES_SYSTEM_REQUIRED);
            if (0 == m_previousExecutionState)
            {
                MessageBox.Show("Call to SetThreadExecutionState failed unexpectedly.",
                    "NoSleep", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // No way to recover; fail gracefully
                Close();
            }
        }

        public class NetworkSetting
        {
            public string name = "";
            public string value = "";
            public NetworkSetting(string name, string value)
            {
                this.name = name;
                this.value = value;
            }

            public NetworkSetting(string item)
            {
                item = item.Trim();
                Match match = Regex.Match(item, @"([^:]+):\s{1,}(.*)");
                Match matchMac = Regex.Match(item, "([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})");
                if (match.Success)
                {
                    this.name = match.Groups[1].Value.Trim().Replace("\"", "");
                    this.value = match.Groups[2].Value.Trim().Replace("\"", "");
                }
                else if (matchMac.Success)
                {
                    this.name = "Device";
                    this.value = (matchMac.Groups[0].Value).Trim().Replace("\"", "");
                }
            }

            public override string ToString()
            {
                if (this.name == "" && this.value == "")
                {
                    return "";
                }
                else
                {
                    return this.name + " = " + this.value;
                }
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Restore previous state
            if (0 == NativeMethods.SetThreadExecutionState(m_previousExecutionState))
            {
                // No way to recover; already exiting
            }
        }

        private void RegisterInStartup(bool isChecked)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey
                    ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (isChecked)
            {
                registryKey.SetValue("VWiFiManager", Application.ExecutablePath);
            }
            else
            {
                registryKey.DeleteValue("VWiFiManager");
            }
        }

        private void setupNetwork()
        {
            if (Properties.Settings.Default.NetworkName.Length < 1)
            {
                Properties.Settings.Default.NetworkName = "RenCloud WiFi - " + (new Random().Next(1000, 9999).ToString());
            }

            if (Properties.Settings.Default.NetworkPass.Length < 8)
            {
                Properties.Settings.Default.NetworkPass = "Pass_" + (new Random().Next(10000, 99999).ToString());
            }

            Properties.Settings.Default.Save();
            inputName.Text = Properties.Settings.Default.NetworkName;
            inputPass.Text = Properties.Settings.Default.NetworkPass;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // this.Hide();
            // this.ShowInTaskbar = false;
            if (Properties.Settings.Default.AppUpgraded)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.AppUpgraded = false;
                Properties.Settings.Default.Save();
            }

            setupNetwork();

            autoStartCheck.Checked = Properties.Settings.Default.AutoStart;

            statusThread.RunWorkerAsync();

            this.Text = String.Format(
                "{0} (Ver: {1})",
                Assembly.GetExecutingAssembly().GetName().FullName,
                Assembly.GetExecutingAssembly().GetName().Version
            );

            new Deps.Update();
        }

        private string startProcess(string filename, string arguments = "")
        {
            System.Diagnostics.Process pProcess = new System.Diagnostics.Process();
            pProcess.StartInfo.FileName = filename;
            pProcess.StartInfo.Arguments = arguments;
            pProcess.StartInfo.UseShellExecute = false;
            pProcess.StartInfo.RedirectStandardOutput = true;
            pProcess.StartInfo.CreateNoWindow = true;
            pProcess.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(866);
            pProcess.Start();
            string strOutput = pProcess.StandardOutput.ReadToEnd();
            pProcess.WaitForExit();

            return strOutput;
        }

        private void statusThread_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (this.WindowState != FormWindowState.Minimized)
                {
                    string strOutput = startProcess("netsh", "wlan show hostednetwork");

                    string[] strOutputArray = strOutput.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    List<NetworkSetting> settings = new List<NetworkSetting>();

                    for (int i = 0; i < strOutputArray.Length; i++)
                    {
                        NetworkSetting ns = new NetworkSetting(strOutputArray[i]);
                        if (ns.ToString() != "")
                        {
                            settings.Add(ns);
                        }
                    }

                    StringBuilder text = new StringBuilder();
                    settings.ForEach((NetworkSetting item) =>
                    {
                        text.AppendLine(item.ToString());
                    });

                    Invoke((MethodInvoker)delegate
                    {
                        statusBox.Text = text.ToString();
                        if (settings.Count < 3)
                        {
                            // wlan host is not available
                            statusBox.Text = strOutput;
                            button1.Visible = false;
                            button2.Visible = false;
                            inputName.Enabled = false;
                            inputPass.Enabled = false;
                        }
                        else if (settings.Count > 6)
                        {
                            // network enabled
                            button1.Visible = false;
                            button2.Visible = true;
                            inputName.Enabled = false;
                            inputPass.Enabled = false;
                        }
                        else
                        {
                            // network disabled
                            button1.Visible = true;
                            button2.Visible = false;
                            inputName.Enabled = true;
                            inputPass.Enabled = true;

                            if (Properties.Settings.Default.AutoStart)
                            {
                                startHostedNetwork();
                                Thread.Sleep(1000);
                            }
                        }
                    });
                }

                Thread.Sleep(500);
            }
        }

        private void startHostedNetwork()
        {
            Properties.Settings.Default.NetworkName = inputName.Text;
            Properties.Settings.Default.NetworkPass = inputPass.Text;

            setupNetwork();

            if (inputName.Text.Length <= 0)
            {
                MessageBox.Show("Network name is too short!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (inputPass.Text.Length < 8)
            {
                MessageBox.Show("Password is too short!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (inputName.Text.IndexOf('"') != -1 || inputPass.Text.IndexOf('"') != -1)
            {
                MessageBox.Show("Symbol '\"' is not allowed in both network name and password!", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string wlanSet = String.Format("wlan set hostednetwork mode=allow \"ssid={0}\" \"key={1}\"",
                inputName.Text,
                inputPass.Text);

            startProcess("netsh", wlanSet);
            startProcess("netsh", "wlan start hostednetwork");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            startHostedNetwork();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            startProcess("netsh", "wlan stop hostednetwork");
            startProcess("netsh", "wlan set hostednetwork mode=disallow");
            autoStartCheck.Checked = false;
        }

        private void trayIcon_DoubleClick(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            this.Show();
            WindowState = FormWindowState.Normal;
            this.BringToFront();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                ShowInTaskbar = false;
            }
        }

        private void autoStartCheck_CheckedChanged(object sender, EventArgs e)
        {
            CheckBox snd = (CheckBox)sender;
            Properties.Settings.Default.AutoStart = snd.Checked;
            RegisterInStartup(snd.Checked);
            Properties.Settings.Default.Save();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            startProcess("IcsManagerGUI.exe");
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(linkLabel1.Text);
        }
    }

    internal static class NativeMethods
    {
        // Import SetThreadExecutionState Win32 API and necessary flags
        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(uint esFlags);
        public const uint ES_CONTINUOUS = 0x80000000;
        public const uint ES_SYSTEM_REQUIRED = 0x00000001;
    }
}
