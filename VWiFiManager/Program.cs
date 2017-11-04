using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VWiFiManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (Mutex mutex = new Mutex(false, "Global\\" + appGuid))
            {
                if (!mutex.WaitOne(0, false))
                {
                    MessageBox.Show("Application is already running!", "VWiFiManager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }


                Application.Run(new MainForm());
            }
        }

        private static string appGuid = "22A5BC39-B524-46BC-B2C5-94F11CE3ED28";
        private static string appTitle = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
    }
}
