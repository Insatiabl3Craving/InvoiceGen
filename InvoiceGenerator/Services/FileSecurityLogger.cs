using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace InvoiceGenerator.Services
{
    /// <summary>
    /// Dual-sink security logger: writes JSON-lines to a file in
    /// <c>%LOCALAPPDATA%/InvoiceGenerator/security.log</c> and echoes
    /// a compact summary to <see cref="Debug.WriteLine"/>.
    /// </summary>
    public sealed class FileSecurityLogger : ISecurityLogger, IDisposable
    {
        private readonly string _logFilePath;
        private readonly object _writeLock = new();
        private StreamWriter? _writer;
        private bool _disposed;

        public FileSecurityLogger(string? logDirectory = null)
        {
            var directory = logDirectory
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "InvoiceGenerator");

            Directory.CreateDirectory(directory);
            _logFilePath = Path.Combine(directory, "security.log");
        }

        /// <summary>
        /// Full path to the active log file (useful for diagnostics / tests).
        /// </summary>
        public string LogFilePath => _logFilePath;

        public void Log(SecurityLogEntry entry)
        {
            if (_disposed)
            {
                return;
            }

            var json = FormatJson(entry);
            var debugLine = FormatDebug(entry);

            lock (_writeLock)
            {
                if (_disposed)
                {
                    return;
                }

                EnsureWriter();
                _writer!.WriteLine(json);
            }

            Debug.WriteLine(debugLine);
        }

        public void Dispose()
        {
            lock (_writeLock)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _writer?.Flush();
                _writer?.Dispose();
                _writer = null;
            }
        }

        // ── Internals ───────────────────────────────────────────────

        private void EnsureWriter()
        {
            _writer ??= new StreamWriter(_logFilePath, append: true, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        // ── Formatters ──────────────────────────────────────────────

        internal static string FormatJson(SecurityLogEntry entry)
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            AppendJsonString(sb, "ts", entry.Timestamp.ToString("o", CultureInfo.InvariantCulture));
            sb.Append(',');
            AppendJsonString(sb, "event", entry.EventType.ToString());

            if (entry.LockCycleId.HasValue)
            {
                sb.Append(',');
                AppendJsonString(sb, "lockCycleId", entry.LockCycleId.Value.ToString("D"));
            }

            if (entry.AttemptId.HasValue)
            {
                sb.Append(',');
                AppendJsonString(sb, "attemptId", entry.AttemptId.Value.ToString("D"));
            }

            if (entry.Message is not null)
            {
                sb.Append(',');
                AppendJsonString(sb, "msg", entry.Message);
            }

            if (entry.Properties is { Count: > 0 })
            {
                sb.Append(",\"props\":{");
                var first = true;
                foreach (var kvp in entry.Properties)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    AppendJsonValue(sb, kvp.Key, kvp.Value);
                }
                sb.Append('}');
            }

            sb.Append('}');
            return sb.ToString();
        }

        internal static string FormatDebug(SecurityLogEntry entry)
        {
            var sb = new StringBuilder(128);
            sb.Append("[Security] ");
            sb.Append(entry.EventType);

            if (entry.LockCycleId.HasValue)
            {
                sb.Append(" cycle=");
                sb.Append(entry.LockCycleId.Value.ToString("D").Substring(0, 8));
            }

            if (entry.AttemptId.HasValue)
            {
                sb.Append(" attempt=");
                sb.Append(entry.AttemptId.Value.ToString("D").Substring(0, 8));
            }

            if (entry.Message is not null)
            {
                sb.Append(" — ");
                sb.Append(entry.Message);
            }

            return sb.ToString();
        }

        // ── JSON helpers (no dependency on System.Text.Json) ─────

        private static void AppendJsonString(StringBuilder sb, string key, string value)
        {
            sb.Append('"');
            sb.Append(EscapeJson(key));
            sb.Append("\":\"");
            sb.Append(EscapeJson(value));
            sb.Append('"');
        }

        private static void AppendJsonValue(StringBuilder sb, string key, object? value)
        {
            sb.Append('"');
            sb.Append(EscapeJson(key));
            sb.Append("\":");

            switch (value)
            {
                case null:
                    sb.Append("null");
                    break;
                case bool b:
                    sb.Append(b ? "true" : "false");
                    break;
                case int i:
                    sb.Append(i.ToString(CultureInfo.InvariantCulture));
                    break;
                case long l:
                    sb.Append(l.ToString(CultureInfo.InvariantCulture));
                    break;
                case double d:
                    sb.Append(d.ToString(CultureInfo.InvariantCulture));
                    break;
                case float f:
                    sb.Append(f.ToString(CultureInfo.InvariantCulture));
                    break;
                case decimal m:
                    sb.Append(m.ToString(CultureInfo.InvariantCulture));
                    break;
                default:
                    sb.Append('"');
                    sb.Append(EscapeJson(value.ToString() ?? string.Empty));
                    sb.Append('"');
                    break;
            }
        }

        private static string EscapeJson(string s)
        {
            if (s.IndexOfAny(new[] { '"', '\\', '\n', '\r', '\t' }) < 0)
            {
                return s;
            }

            var sb = new StringBuilder(s.Length + 8);
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"':  sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n");  break;
                    case '\r': sb.Append("\\r");  break;
                    case '\t': sb.Append("\\t");  break;
                    default:   sb.Append(c);      break;
                }
            }
            return sb.ToString();
        }
    }
}
