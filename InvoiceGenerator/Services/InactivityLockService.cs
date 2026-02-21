using System;
using System.Diagnostics;
using System.Threading;

namespace InvoiceGenerator.Services
{
    public class InactivityLockService : IDisposable
    {
        private readonly TimeSpan _timeout;
        private readonly Timer _timer;
        private readonly object _syncRoot = new();
        private bool _isRunning;
        private bool _disposed;
        private long _lastActivityStamp;

        public event EventHandler? TimeoutElapsed;

        public InactivityLockService(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Inactivity timeout must be greater than zero.");
            }

            _timeout = timeout;
            _timer = new Timer(Timer_Tick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            _lastActivityStamp = Stopwatch.GetTimestamp();
        }

        public void Start()
        {
            ThrowIfDisposed();

            lock (_syncRoot)
            {
                _lastActivityStamp = Stopwatch.GetTimestamp();
                _isRunning = true;
                _timer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            }
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _isRunning = false;
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        public void RegisterActivity()
        {
            if (_disposed)
            {
                return;
            }

            Interlocked.Exchange(ref _lastActivityStamp, Stopwatch.GetTimestamp());
        }

        private void Timer_Tick(object? state)
        {
            if (_disposed || !_isRunning)
            {
                return;
            }

            var elapsed = GetElapsedSinceLastActivity();
            if (elapsed < _timeout)
            {
                return;
            }

            lock (_syncRoot)
            {
                if (_disposed || !_isRunning)
                {
                    return;
                }

                _isRunning = false;
                _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            TimeoutElapsed?.Invoke(this, EventArgs.Empty);
        }

        private TimeSpan GetElapsedSinceLastActivity()
        {
            var currentStamp = Stopwatch.GetTimestamp();
            var lastStamp = Interlocked.Read(ref _lastActivityStamp);
            var elapsedTicks = currentStamp - lastStamp;

            if (elapsedTicks <= 0)
            {
                return TimeSpan.Zero;
            }

            var seconds = (double)elapsedTicks / Stopwatch.Frequency;
            return TimeSpan.FromSeconds(seconds);
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _isRunning = false;
            }

            _timer.Dispose();
            GC.SuppressFinalize(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(InactivityLockService));
            }
        }
    }
}
