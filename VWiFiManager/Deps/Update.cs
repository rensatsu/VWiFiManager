using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using VWiFiManager.Deps;

namespace VWiFiManager.Deps
{
    class Update
    {
        private string versionInfoUrl = "https://raw.githubusercontent.com/rensatsu/VWiFiManager/master/version.txt";
        public Update()
        {
            Thread thread = new Thread(new ThreadStart(CheckUpdate));
            thread.Start();
        }

        public void DownloadUpdate(string url, string hash)
        {
            string tmpDir = System.IO.Path.GetTempPath();
            string tmpName = tmpDir + "VWiFiManager_Update.exe";
            WebClient Client = new WebClient();
            Client.DownloadFile(url, tmpName);

            HashCheck hashCheck = new HashCheck(tmpName, hash);
            bool verifiedHash = hashCheck.Result();

            if (verifiedHash)
            {
                System.Diagnostics.Process.Start(tmpName, "/SILENT /CLOSEAPPLICATIONS /NOICONS /SP-");
                Application.Exit();
            }
            return;
        }
        
        public void CheckUpdate()
        {
            try
            {
                WebClient client = new WebClient();
                string content = string.Empty;
                Stream stream;

                try
                {
                    stream = client.OpenRead(versionInfoUrl);
                    StreamReader reader = new StreamReader(stream);
                    content = reader.ReadToEnd();
                }
                catch (WebException)
                {
                    // MessageBox.Show(ex.Message);
                    return;
                }

                string[] strContent = content.Split(';');
                if (strContent.Length != 3)
                {
                    // MessageBox.Show("Text file must be in this format \"version;link;hash\"");
                    return;
                }

                Version updVersion = new Version(strContent[0]);
                string updLink = strContent[1];
                string hashStr = strContent[2];

                Version curVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                if (updVersion > curVersion)
                {
                    /*
                    string msg = String.Format(
                        "New version available!\nCurrent version: {0}\nVersion on server: {1}\nDo you want to update?",
                        curVersion,
                        updVersion);
                    DialogResult res = MessageBox.Show(
                        msg,
                        "VK Player :: Update",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    if (res == DialogResult.Yes)
                    {
                        DownloadUpdate(updLink, hashStr);
                    }
                    */
                    string msg = String.Format(
                        "New version available! Click 'OK' to update.\nCurrent version: {0}\nVersion on server: {1}",
                        curVersion,
                        updVersion);
                    DialogResult res = MessageBox.Show(
                        msg,
                        "VWiFiManager :: Update",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    DownloadUpdate(updLink, hashStr);
                }
                return;
            }
            catch
            {
                return;
            }
        }
    }
}
