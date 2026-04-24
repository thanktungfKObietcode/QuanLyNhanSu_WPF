using System;
using System.Windows;
using System.Windows.Threading;
using QuanLyNhanSu_WPF.Models;

namespace QuanLyNhanSu_WPF.Helpers
{
    public sealed class SessionManager
    {
        private static readonly Lazy<SessionManager> _instance = new(() => new SessionManager());
        public static SessionManager Instance => _instance.Value;

        private SessionManager() { }

        public User CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;
        public DateTime? LoginTime { get; private set; }
        private DateTime? _lastActivity;

        private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(30);
        private DispatcherTimer _idleTimer;

        /// <summary>Raised when the session times out due to inactivity.</summary>
        public event Action SessionTimedOut;

        public void StartSession(User user)
        {
            CurrentUser = user;
            LoginTime = DateTime.UtcNow;
            _lastActivity = DateTime.UtcNow;
            StartIdleTimer();
        }

        public void EndSession()
        {
            CurrentUser = null;
            LoginTime = null;
            _lastActivity = null;
            _idleTimer?.Stop();
        }

        /// <summary>Call on any user interaction (mouse, key) to reset idle timer.</summary>
        public void RefreshActivity()
        {
            if (IsLoggedIn)
            {
                _lastActivity = DateTime.UtcNow;
                _idleTimer?.Stop();
                _idleTimer?.Start();
            }
        }

        public bool IsSessionExpired()
        {
            if (!IsLoggedIn || !_lastActivity.HasValue) return true;
            return DateTime.UtcNow - _lastActivity.Value > IdleTimeout;
        }

        // Legacy method name kept for compatibility
        public void RefreshSession() => RefreshActivity();
        public bool CheckSessionTimeout() => IsSessionExpired();

        private void StartIdleTimer()
        {
            _idleTimer?.Stop();
            _idleTimer = new DispatcherTimer
            {
                Interval = IdleTimeout
            };
            _idleTimer.Tick += (s, e) =>
            {
                _idleTimer.Stop();
                if (IsLoggedIn)
                {
                    EndSession();
                    SessionTimedOut?.Invoke();
                }
            };
            _idleTimer.Start();
        }
    }
}
