using CodeDeck.Windows.Properties;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CodeDeck.Windows
{
    internal class Program
    {
        private static NotifyIcon _ni = new();
        private static ContextMenuStrip _cms = new();
        private static PluginTaskWindow? _pluginTaskWindow;
        private static IntPtr _consoleWindow;

        public static void Main(string[] args)
        {
            var debugConsoleRequested = Debugger.IsAttached
                || args.Contains("--debug", StringComparer.OrdinalIgnoreCase);

            try
            {
                InitializeDebugConsole(debugConsoleRequested);
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show($"Could not initialize the debug console. Error: \"{ex.Message}\"\nUse Windows Console Host rather than Windows Terminal.",
                    "Debug Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = 1;
                return;
            }

            var hostTask = CodeDeck.Program.Main(args);
            if (hostTask.IsFaulted)
            {
                MessageBox.Show($"Code Deck could not start. Error: \"{hostTask.Exception?.GetBaseException().Message}\"",
                    "Startup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.ExitCode = 1;
                return;
            }
            var logger = CodeDeck.Program.CreateLogger("CodeDeck.Windows");

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
                    logger.LogError(ex, "Could not open the configuration file.");
                    MessageBox.Show($"Could not open configuration file. Error: \"{ex.Message}\"",
                        "Open Configuration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var mnuItemPluginTasks = _cms.Items.Add("Plugin Tasks...");
            mnuItemPluginTasks.Click += (_, _) => ShowPluginTasks();

            var mnuItemDebugLog = _cms.Items.Add("Show Debug Log");
            _cms.Opening += (_, _) =>
                mnuItemDebugLog.Text = IsWindowVisible(_consoleWindow) ? "Hide Debug Log" : "Show Debug Log";
            mnuItemDebugLog.Click += (_, _) =>
            {
                try
                {
                    SetDebugConsoleVisible(!IsWindowVisible(_consoleWindow));
                }
                catch (Win32Exception ex)
                {
                    logger.LogError(ex, "Could not change debug console visibility.");
                    MessageBox.Show($"Could not change debug console visibility. Error: \"{ex.Message}\"",
                        "Debug Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

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
                    logger.LogError(ex, "Host shutdown failed.");
                    MessageBox.Show($"Host shutdown failed. Error: \"{ex.Message}\"",
                        "Exit", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private static void InitializeDebugConsole(bool visible)
        {
            if (GetConsoleWindow() != IntPtr.Zero && !FreeConsole())
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Use our own console, allocated before the host creates its console logger.
            if (!AllocConsole()) throw new Win32Exception(Marshal.GetLastWin32Error());
            var window = GetConsoleWindow();
            if (window == IntPtr.Zero)
            {
                throw new Win32Exception("The console has no window.");
            }

            var menu = GetSystemMenu(window, false);
            if (menu == IntPtr.Zero)
            {
                throw new Win32Exception("The console has no system menu.");
            }

            const uint closeCommand = 0xF060;
            const uint grayed = 0x0001;
            if (EnableMenuItem(menu, closeCommand, grayed) == -1)
            {
                throw new Win32Exception("The console Close command could not be disabled.");
            }
            if (!DrawMenuBar(window)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!SetConsoleTitle("CodeDeck Debug Log")) throw new Win32Exception(Marshal.GetLastWin32Error());

            _consoleWindow = window;
            SetDebugConsoleVisible(visible);
        }

        private static void SetDebugConsoleVisible(bool visible)
        {
            const int hide = 0;
            const int restore = 9;
            ShowWindow(_consoleWindow, visible ? restore : hide);
            if (IsWindowVisible(_consoleWindow) != visible)
            {
                throw new Win32Exception("The console window did not change to the requested visibility.");
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetConsoleTitle(string title);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr window, bool revert);

        [DllImport("user32.dll")]
        private static extern int EnableMenuItem(IntPtr menu, uint item, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DrawMenuBar(IntPtr window);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr window);
    }
}
