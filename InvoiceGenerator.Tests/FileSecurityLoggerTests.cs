using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InvoiceGenerator.Services;
using Xunit;

namespace InvoiceGenerator.Tests;

public class FileSecurityLoggerTests : IDisposable
{
    private readonly string _tempDir;

    public FileSecurityLoggerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "InvGenTests_" + Guid.NewGuid().ToString("N")[..8]);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void Log_WritesValidJsonLineToFile()
    {
        using var logger = new FileSecurityLogger(_tempDir);

        logger.Log(new SecurityLogEntry
        {
            EventType = SecurityEventType.AppShutdown,
            Message = "test message"
        });

        logger.Dispose(); // flush

        var lines = File.ReadAllLines(logger.LogFilePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        Assert.Single(lines);
        var json = lines[0];
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
        Assert.Contains("\"event\":\"AppShutdown\"", json);
        Assert.Contains("\"msg\":\"test message\"", json);
        Assert.Contains("\"ts\":", json);
    }

    [Fact]
    public void Log_IncludesCorrelationIds_WhenProvided()
    {
        using var logger = new FileSecurityLogger(_tempDir);
        var cycleId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();

        logger.Log(new SecurityLogEntry
        {
            EventType = SecurityEventType.UnlockAttemptStarted,
            LockCycleId = cycleId,
            AttemptId = attemptId,
            Message = "unlock attempt"
        });

        logger.Dispose();

        var json = File.ReadAllText(logger.LogFilePath);
        Assert.Contains($"\"lockCycleId\":\"{cycleId:D}\"", json);
        Assert.Contains($"\"attemptId\":\"{attemptId:D}\"", json);
    }

    [Fact]
    public void Log_OmitsCorrelationIds_WhenNull()
    {
        using var logger = new FileSecurityLogger(_tempDir);

        logger.Log(new SecurityLogEntry
        {
            EventType = SecurityEventType.PasswordSet,
            Message = "password set"
        });

        logger.Dispose();

        var json = File.ReadAllText(logger.LogFilePath);
        Assert.DoesNotContain("lockCycleId", json);
        Assert.DoesNotContain("attemptId", json);
    }

    [Fact]
    public void Log_WritesProperties_AsNestedJsonObject()
    {
        using var logger = new FileSecurityLogger(_tempDir);

        logger.Log(new SecurityLogEntry
        {
            EventType = SecurityEventType.LockoutStarted,
            Message = "lockout",
            Properties = new Dictionary<string, object?>
            {
                ["FailedAttempts"] = 5,
                ["LockoutDurationSeconds"] = 300.0,
                ["NullValue"] = null,
                ["BoolValue"] = true
            }
        });

        logger.Dispose();

        var json = File.ReadAllText(logger.LogFilePath);
        Assert.Contains("\"props\":{", json);
        Assert.Contains("\"FailedAttempts\":5", json);
        Assert.Contains("\"LockoutDurationSeconds\":300", json);
        Assert.Contains("\"NullValue\":null", json);
        Assert.Contains("\"BoolValue\":true", json);
    }

    [Fact]
    public void Log_MultipleEntries_AppendsLines()
    {
        using var logger = new FileSecurityLogger(_tempDir);

        logger.Log(new SecurityLogEntry { EventType = SecurityEventType.AppStartupAuth, Message = "first" });
        logger.Log(new SecurityLogEntry { EventType = SecurityEventType.AppShutdown, Message = "second" });

        logger.Dispose();

        var lines = File.ReadAllLines(logger.LogFilePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        Assert.Equal(2, lines.Count);
        Assert.Contains("AppStartupAuth", lines[0]);
        Assert.Contains("AppShutdown", lines[1]);
    }

    [Fact]
    public void NullSecurityLogger_DoesNotThrow()
    {
        var logger = NullSecurityLogger.Instance;

        var ex = Record.Exception(() => logger.Log(new SecurityLogEntry
        {
            EventType = SecurityEventType.HandlerException,
            Message = "should be ignored"
        }));

        Assert.Null(ex);
    }

    [Fact]
    public void FormatJson_EscapesSpecialCharacters()
    {
        var entry = new SecurityLogEntry
        {
            EventType = SecurityEventType.HandlerException,
            Message = "Error with \"quotes\" and\nnewlines"
        };

        var json = FileSecurityLogger.FormatJson(entry);

        Assert.Contains("\\\"quotes\\\"", json);
        Assert.Contains("\\n", json);
        Assert.DoesNotContain("\n", json.Replace("\\n", ""));
    }

    [Fact]
    public void FormatDebug_ShowsTruncatedCorrelationIds()
    {
        var cycleId = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
        var entry = new SecurityLogEntry
        {
            EventType = SecurityEventType.UnlockSuccess,
            LockCycleId = cycleId,
            Message = "Unlock succeeded."
        };

        var debug = FileSecurityLogger.FormatDebug(entry);

        Assert.StartsWith("[Security] UnlockSuccess", debug);
        Assert.Contains("cycle=01234567", debug);
        Assert.Contains("Unlock succeeded.", debug);
    }
}
