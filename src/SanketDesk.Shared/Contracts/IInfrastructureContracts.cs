#nullable enable
using System;

namespace SanketDesk
{
    /// <summary>
    /// Thread dispatcher abstraction for safely marshaling execution to the host WinForms UI thread.
    /// Worker threads MUST NEVER touch controls directly.
    /// </summary>
    public interface IUiDispatcher
    {
        /// <summary>
        /// Returns true if the calling thread is the UI thread owning the controls.
        /// </summary>
        bool CheckAccess();

        /// <summary>
        /// Posts an action asynchronously to the UI message loop.
        /// Does NOT block the caller.
        /// </summary>
        void Post(Action action);

        /// <summary>
        /// Sends an action synchronously to the UI message loop if on another thread.
        /// If already on the UI thread, executes immediately.
        /// </summary>
        void Send(Action action);
    }

    /// <summary>
    /// Clock abstraction for deterministic time retrieval, duration tracking, and testability.
    /// </summary>
    public interface ISystemClock
    {
        DateTime UtcNow { get; }
        long MonotonicMilliseconds { get; }
    }

    /// <summary>
    /// Default production clock using standard high-precision system time and stopwatch timestamps.
    /// </summary>
    public sealed class SystemClock : ISystemClock
    {
        public static readonly SystemClock Instance = new SystemClock();

        private static readonly long StartTick = System.Diagnostics.Stopwatch.GetTimestamp();
        private static readonly double TickToMs = 1000.0 / System.Diagnostics.Stopwatch.Frequency;

        public DateTime UtcNow => DateTime.UtcNow;

        public long MonotonicMilliseconds
        {
            get
            {
                long diff = System.Diagnostics.Stopwatch.GetTimestamp() - StartTick;
                return (long)(diff * TickToMs);
            }
        }
    }

    /// <summary>
    /// Lightweight diagnostic logger for internal library warnings and non-fatal errors.
    /// Does not require any external logging framework.
    /// </summary>
    public interface IDiagnosticLogger
    {
        void LogDebug(string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message, Exception? ex = null);
    }

    /// <summary>
    /// Default silent logger that suppresses diagnostic output or writes to debug listener.
    /// </summary>
    public sealed class NullDiagnosticLogger : IDiagnosticLogger
    {
        public static readonly NullDiagnosticLogger Instance = new NullDiagnosticLogger();

        public void LogDebug(string message) { }
        public void LogInfo(string message) { }
        public void LogWarning(string message) { }
        public void LogError(string message, Exception? ex = null) { }
    }
}
