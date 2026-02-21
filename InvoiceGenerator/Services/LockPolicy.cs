using System;

namespace InvoiceGenerator.Services
{
    public enum LockDecision
    {
        Allow,
        Defer,
        Skip
    }

    public sealed class AppLockStateSnapshot
    {
        public bool IsAlreadyLocked { get; init; }
        public bool HasBlockingModal { get; init; }
        public bool IsShuttingDown { get; init; }
        public bool SessionIsLocked { get; init; }
        public bool MainWindowReady { get; init; }
        public bool MainWindowActive { get; init; }
        public int VisibleWindowCount { get; init; }
    }

    public interface ILockPolicyEvaluator
    {
        LockDecision Evaluate(AppLockStateSnapshot state);
    }

    public sealed class DefaultLockPolicyEvaluator : ILockPolicyEvaluator
    {
        public LockDecision Evaluate(AppLockStateSnapshot state)
        {
            if (state.IsShuttingDown || !state.MainWindowReady)
            {
                return LockDecision.Skip;
            }

            if (state.IsAlreadyLocked)
            {
                return LockDecision.Skip;
            }

            if (state.SessionIsLocked || !state.MainWindowActive)
            {
                return LockDecision.Defer;
            }

            if (state.HasBlockingModal || state.VisibleWindowCount > 1)
            {
                return LockDecision.Defer;
            }

            return LockDecision.Allow;
        }
    }
}
