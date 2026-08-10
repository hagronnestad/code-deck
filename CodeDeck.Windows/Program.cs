using CodeDeck.Windows.Properties;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodeDeck.Windows
{
    internal class Program
    {
        private static NotifyIcon _ni = new();
        private static ContextMenuStrip _cms = new();
        private static PluginTaskWindow? _pluginTaskWindow;

        public static void Main(string[] args)
        {
            var debugConsoleRequested = Debugger.IsAttached
                || args.Contains("--debug", StringComparer.OrdinalIgnoreCase);

            if (debugConsoleRequested && AllocConsole())
            {
                Console.Title = "CodeDeck Debug Log";
            }

            var hostTask = CodeDeck.Program.Main(args);

            using var applicationContext = new ApplicationContext();

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
                        FileName = ConfigurationProvider.ConfigFile,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not open configuration file. Error: \"{ex.Message}\"",
                        "Open Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var mnuItemPluginTasks = _cms.Items.Add("Plugin Tasks...");
            mnuItemPluginTasks.Click += (_, _) => ShowPluginTasks();

            var mnuItemExit = _cms.Items.Add("Exit");
            mnuItemExit.Click += (s, e) =>
            {
                _pluginTaskWindow?.Close();
                _ni.Visible = false;
                CodeDeck.Program.StopHost();

                // Give the host a chance to run its ordered shutdown (tile DeInit,
                // plugin task cancellation and assembly load context unloading)
                // before the process goes away.
                try
                {
                    hostTask.Wait(TimeSpan.FromSeconds(15));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Host shutdown failed: {ex.Message}");
                }

                applicationContext.ExitThread();
            };

            Application.Run(applicationContext);
        }

        private static void _ni_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _cms.Show();
            }
        }

        private static void ShowPluginTasks()
        {
            if (_pluginTaskWindow is null || _pluginTaskWindow.IsDisposed)
            {
                _pluginTaskWindow = new PluginTaskWindow(() => _pluginTaskWindow = null);
                _pluginTaskWindow.Show();
                return;
            }

            if (_pluginTaskWindow.WindowState == FormWindowState.Minimized)
            {
                _pluginTaskWindow.WindowState = FormWindowState.Normal;
            }

            _pluginTaskWindow.Activate();
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
