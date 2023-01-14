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
            _ni.Icon = SystemIcons.Application;
            _ni.MouseUp += _ni_MouseClick;

            var mnuItemExit = _cms.Items.Add("Exit");
            mnuItemExit.Click += (s, e) =>
            {
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
