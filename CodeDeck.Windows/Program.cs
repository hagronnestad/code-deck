using CodeDeck.Windows.Properties;
using System.Diagnostics;

namespace CodeDeck.Windows
{
    internal class Program
    {
        private static NotifyIcon _ni = new();
        private static ContextMenuStrip _cms = new();

        public static void Main(string[] args)
        {
            _ = CodeDeck.Program.Main(args);

            var f = new Form() {
                Visible = false,
                ShowInTaskbar = false,
                WindowState = FormWindowState.Minimized
            };
            f.Resize += (s, e) => { if (f.WindowState == FormWindowState.Minimized) f.Hide(); };

            _ni.Visible = true;
            _ni.ContextMenuStrip = _cms;
            _ni.Icon = Resources.icon_16_white_outline;
            _ni.MouseUp += _ni_MouseClick;

            var mnuItemOpenConf = _cms.Items.Add("Open Configuration");
            mnuItemOpenConf.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = ConfigurationProvider.CONFIGURATION_FILE_NAME,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open configuration file. Error: \"{ex.Message}\"",
                        "Open Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var mnuItemExit = _cms.Items.Add("Exit");
            mnuItemExit.Click += (s, e) =>
            {
                _ni.Visible = false;
                CodeDeck.Program.StopHost();
                f.Close();
                Application.Exit();
            };

            Application.Run(f);
        }

        private static void _ni_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _cms.Show();
            }
        }
    }
}
