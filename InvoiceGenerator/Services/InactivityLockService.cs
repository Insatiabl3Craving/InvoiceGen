using System;
using System.Windows.Threading;

namespace InvoiceGenerator.Services
{
    public class InactivityLockService
    {
        private readonly TimeSpan _timeout;
        private readonly DispatcherTimer _timer;
        private DateTime _lastActivityUtc;

        public event EventHandler? TimeoutElapsed;

        public InactivityLockService(TimeSpan timeout)
        {
            _timeout = timeout;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            _lastActivityUtc = DateTime.UtcNow;
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }

        public void Stop()
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
            }
        }

        public void RegisterActivity()
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.UtcNow - _lastActivityUtc;
            if (elapsed < _timeout)
            {
                return;
            }

            Stop();
            TimeoutElapsed?.Invoke(this, EventArgs.Empty);
        }
    }
}
