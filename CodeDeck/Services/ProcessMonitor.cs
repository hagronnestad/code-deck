using System.Diagnostics;
using System.Timers;

namespace CodeDeck.Services
{
    public class ProcessMonitor
    {
        public event EventHandler<string>? ProcessStarted;
        public event EventHandler<string>? ProcessExited;

        private readonly Dictionary<string, bool> _monitoredProcesses = new(StringComparer.OrdinalIgnoreCase);
        private readonly System.Timers.Timer _timer = new();

        public ProcessMonitor()
        {
            _timer.Elapsed += HandleTimerElapsed;
        }

        public void Start()
        {
            _timer.Interval = 500;
            _timer.AutoReset = true;
            _timer.Enabled = true;
        }

        public void Stop()
        {
            _timer.Enabled = false;
        }

        public void Add(string name)
        {
            _monitoredProcesses.TryAdd(name, false);
        }

        public void Remove(string name)
        {
            _monitoredProcesses.Remove(name);
        }

        public void Clear()
        {
            _monitoredProcesses.Clear();
        }

        private void HandleTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            UpdateMonitoredProcesses();
        }

        private void UpdateMonitoredProcesses()
        {
            if (_monitoredProcesses.Count == 0) return;

            var runningProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var process in Process.GetProcesses())
            {
                runningProcesses.Add(process.ProcessName);
            }

            foreach (var monitoredProcess in _monitoredProcesses.Keys.ToList())
            {
                var isRunning = runningProcesses.Contains(monitoredProcess);
                if (isRunning == _monitoredProcesses[monitoredProcess]) continue;

                _monitoredProcesses[monitoredProcess] = isRunning;

                if (isRunning) ProcessStarted?.Invoke(this, monitoredProcess);
                else ProcessExited?.Invoke(this, monitoredProcess);
            }
        }
    }
}
