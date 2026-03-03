using System.Drawing;
using System.Windows.Forms;

namespace Contab.WinForms.Forms;

public sealed class SplashForm : Form
{
    private readonly System.Windows.Forms.Timer _timer;

    public SplashForm()
    {
        Text = "Sage Contab";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Width = 700;
        Height = 180;
        BackColor = Color.White;

        var title = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Top,
            Height = 80,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Text = "Sage Contab"
        };

        var subtitle = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            TextAlign = ContentAlignment.TopCenter,
            Text = "C# Windows Application (VB6 migration baseline)"
        };

        Controls.Add(subtitle);
        Controls.Add(title);

        _timer = new System.Windows.Forms.Timer { Interval = 2200 };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            var main = new MainForm();
            main.FormClosed += (_, _) => Close();
            main.Show();
            Hide();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _timer.Start();
    }
}
