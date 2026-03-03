using System;
using System.Windows.Forms;
using Contab.WinForms.Forms;

namespace Contab.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new SplashForm());
    }
}
