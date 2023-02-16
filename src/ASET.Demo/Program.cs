using System;
using System.Windows.Forms;

namespace ASET.Demo
{
    internal static class Program
    {
        public static FrmMain fm;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
#if NET6_0_OR_GREATER
            ApplicationConfiguration.Initialize();
#endif
            Application.Run(fm = new FrmMain());
        }
    }
}