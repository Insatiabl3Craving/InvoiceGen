using System;
using System.Reflection;
using InvoiceGenerator.Services;
using Xunit;

namespace InvoiceGenerator.Tests;

public class InactivityLockServiceTests
{
    [Fact]
    public void Constructor_WithZeroTimeout_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InactivityLockService(TimeSpan.Zero));
    }

    [Fact]
    public void TimerTick_UsesMonotonicTimestamps_IgnoringWallClockJumps()
    {
        var timestampProvider = new FakeTimestampProvider();
        using var service = new InactivityLockService(TimeSpan.FromSeconds(5), timestampProvider);

        var timeoutRaised = false;
        service.TimeoutElapsed += (_, _) => timeoutRaised = true;

        service.Start();

        var simulatedWallClock = DateTime.UtcNow;

        timestampProvider.AdvanceSeconds(4.9);
        simulatedWallClock = simulatedWallClock.AddHours(2);
        InvokeTimerTick(service);
        Assert.False(timeoutRaised);

        simulatedWallClock = simulatedWallClock.AddHours(-4);
        timestampProvider.AdvanceSeconds(0.2);
        InvokeTimerTick(service);

        Assert.True(timeoutRaised);
    }

    [Fact]
    public void RegisterActivity_ResetsInactivityWindow()
    {
        var timestampProvider = new FakeTimestampProvider();
        using var service = new InactivityLockService(TimeSpan.FromSeconds(5), timestampProvider);

        var timeoutRaised = false;
        service.TimeoutElapsed += (_, _) => timeoutRaised = true;

        service.Start();
        timestampProvider.AdvanceSeconds(4);
        InvokeTimerTick(service);
        Assert.False(timeoutRaised);

        service.RegisterActivity();
        timestampProvider.AdvanceSeconds(2);
        InvokeTimerTick(service);
        Assert.False(timeoutRaised);

        timestampProvider.AdvanceSeconds(3.1);
        InvokeTimerTick(service);
        Assert.True(timeoutRaised);
    }

    private static void InvokeTimerTick(InactivityLockService service)
    {
        var tickMethod = typeof(InactivityLockService).GetMethod("Timer_Tick", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(tickMethod);
        tickMethod!.Invoke(service, new object?[] { null });
    }

    private sealed class FakeTimestampProvider : ITimestampProvider
    {
        public long Frequency => 1000;

        public long CurrentStamp { get; private set; }

        public long GetTimestamp() => CurrentStamp;

        public void AdvanceSeconds(double seconds)
        {
            CurrentStamp += (long)Math.Round(seconds * Frequency, MidpointRounding.AwayFromZero);
        }
    }
}
