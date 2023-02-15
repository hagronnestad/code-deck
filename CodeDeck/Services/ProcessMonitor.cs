using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace CodeDeck.Services
{
    public class ProcessMonitor
    {
        public event EventHandler<string>? ProcessStarted;
        public event EventHandler<string>? ProcessExited;

        private readonly Dictionary<string, bool> _monitoredProcesses = new();
        private readonly Timer _timer = new();

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

        public void Add(string name)
        {
            if (_monitoredProcesses.ContainsKey(name)) return;
            _monitoredProcesses.Add(name.ToLower(), false);
        }

        public void Remove(string name)
        {
            if (!_monitoredProcesses.ContainsKey(name)) return;
            _monitoredProcesses.Remove(name.ToLower());
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

            var runningProcesses = Process.GetProcesses()
                .Select(x => x.ProcessName.ToLower())
                .Distinct()
                .ToList();

            foreach (var monitoredProcess in _monitoredProcesses.Keys)
            {
                if (runningProcesses.Contains(monitoredProcess))
                // The monitored process is running
                {
                    if (!_monitoredProcesses[monitoredProcess])
                    // Monitored process is not marked as running
                    {
                        // Mark process as running and invoke event
                        _monitoredProcesses[monitoredProcess] = true;
                        ProcessStarted?.Invoke(this, monitoredProcess);
                    }
                }
                else
                // The monitored process is NOT running
                {
                    // Continue if the monitored process is already marked as not running
                    if (!_monitoredProcesses[monitoredProcess]) continue;

                    // Monitored process is marked as running, mark as NOT running and invoke event
                    _monitoredProcesses[monitoredProcess] = false;
                    ProcessExited?.Invoke(this, monitoredProcess);
                }
            }
        }
    }
}
