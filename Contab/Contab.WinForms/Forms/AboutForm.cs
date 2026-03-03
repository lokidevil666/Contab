using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Contab.WinForms.Forms;

public sealed class AboutForm : Form
{
    public AboutForm()
    {
        Text = "Sage Contab - About";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Width = 620;
        Height = 360;

        var title = new Label
        {
            Left = 20,
            Top = 20,
            Width = 560,
            Height = 40,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(58, 88, 39),
            Text = "Sage Contab"
        };

        var body = new Label
        {
            Left = 20,
            Top = 80,
            Width = 560,
            Height = 140,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            Text = "This version is a C# WinForms migration baseline from the original VB6 project.\n\n" +
                   "Key improvements:\n" +
                   "• Async SQL loading and parameterized queries\n" +
                   "• Reduced global state with service-based architecture\n" +
                   "• Structured fixed-width exporter driven by contab.str"
        };

        var version = new Label
        {
            Left = 20,
            Top = 230,
            Width = 560,
            Height = 24,
            Text = $"Version: {Application.ProductVersion}"
        };

        var systemInfo = new Button
        {
            Left = 340,
            Top = 270,
            Width = 120,
            Height = 30,
            Text = "System Info"
        };
        systemInfo.Click += (_, _) => OpenSystemInfo();

        var close = new Button
        {
            Left = 470,
            Top = 270,
            Width = 90,
            Height = 30,
            Text = "OK",
            DialogResult = DialogResult.OK
        };

        Controls.Add(title);
        Controls.Add(body);
        Controls.Add(version);
        Controls.Add(systemInfo);
        Controls.Add(close);

        AcceptButton = close;
    }

    private static void OpenSystemInfo()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "msinfo32.exe"),
            "msinfo32.exe"
        };

        foreach (var candidate in candidates)
        {
            try
            {
                var info = new ProcessStartInfo(candidate) { UseShellExecute = true };
                Process.Start(info);
                return;
            }
            catch
            {
                // Try next candidate.
            }
        }

        MessageBox.Show("System Information is unavailable at this time.", "Sage Contab", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
