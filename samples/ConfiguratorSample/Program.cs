#nullable enable
using System;
using System.Windows.Forms;

namespace ConfiguratorSample
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// Compatible with both .NET 8 and .NET Framework 4.6.1.
        /// </summary>
        [STAThread]
        private static void Main()
        {
#if NET8_0_OR_GREATER
            ApplicationConfiguration.Initialize();
#else
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
#endif
            Application.Run(new ConfiguratorForm());
        }
    }
}
