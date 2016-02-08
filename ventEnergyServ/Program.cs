using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace ventEnergy
{
    static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Process curProc;
            Process[] proc;
            proc = Process.GetProcesses();
            curProc = Process.GetCurrentProcess();
            foreach(Process pr in proc)
            {
                if (pr.ProcessName == curProc.ProcessName && pr.Id != curProc.Id)
                    curProc.Kill();
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainServerForm(args));
        }
    }
}
