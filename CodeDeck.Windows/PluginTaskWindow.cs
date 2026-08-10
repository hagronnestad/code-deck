using CodeDeck.PluginAbstractions;

namespace CodeDeck.Windows;

internal sealed class PluginTaskWindow : Form
{
    private readonly Action _closed;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly ListView _plugins = CreateListView("Plugin", "Status", "Artifact size");
    private readonly ListView _tasks = CreateListView(
        "Plugin",
        "Owner / tile",
        "Task",
        "Status",
        "Interval",
        "Last start",
        "Duration",
        "Last success",
        "Error");

    public PluginTaskWindow(Action closed)
    {
        _closed = closed;
        Text = "CodeDeck Plugin / Task Manager";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1100;
        Height = 520;

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreatePage("Plugins", _plugins));
        tabs.TabPages.Add(CreatePage("Tasks", _tasks));
        Controls.Add(tabs);

        _refreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };
        _refreshTimer.Tick += (_, _) => RefreshData();
        Shown += (_, _) =>
        {
            RefreshData();
            _refreshTimer.Start();
        };
        FormClosed += (_, _) =>
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            _closed();
        };
    }

    private void RefreshData()
    {
        var snapshot = CodeDeck.Program.GetPluginRuntimeSnapshot();

        _plugins.BeginUpdate();
        _plugins.Items.Clear();
        foreach (var plugin in snapshot.Plugins)
        {
            _plugins.Items.Add(new ListViewItem(new[]
            {
                plugin.Name,
                plugin.IsLoaded ? "Loaded" : "Not loaded",
                FormatBytes(plugin.ArtifactSizeBytes)
            }));
        }
        _plugins.EndUpdate();

        _tasks.BeginUpdate();
        _tasks.Items.Clear();
        foreach (var task in snapshot.Tasks)
        {
            _tasks.Items.Add(new ListViewItem(new[]
            {
                task.PluginName,
                task.Owner,
                task.Name,
                task.State.ToString(),
                task.Interval?.ToString() ?? "Continuous",
                FormatTime(task.LastStartedAt),
                task.LastDuration?.ToString(@"mm\:ss\.fff") ?? string.Empty,
                FormatTime(task.LastSucceededAt),
                task.LastError ?? string.Empty
            }));
        }
        _tasks.EndUpdate();
    }

    private static TabPage CreatePage(string title, Control control)
    {
        var page = new TabPage(title);
        control.Dock = DockStyle.Fill;
        page.Controls.Add(control);
        return page;
    }

    private static ListView CreateListView(params string[] columns)
    {
        var list = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        foreach (var column in columns)
        {
            list.Columns.Add(column, 130);
        }
        return list;
    }

    private static string FormatTime(DateTimeOffset? value) =>
        value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:N1} KB";
        return $"{bytes / (1024d * 1024d):N1} MB";
    }
}
