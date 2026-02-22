using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using InvoiceGenerator.Models;

namespace InvoiceGenerator.Services
{
    public class CsvImportService
    {
        // Required CSV column headers
        private static readonly string[] RequiredHeaders = { "Date", "Day", "Duration", "No of Carers", "Care Description", "Rate (£)" };

        /// <summary>
        /// Imports invoice line items from a CSV file.
        /// Expected columns: Date, Day, Duration, No of Carers, Care Description, Rate (£)
        /// </summary>
        public List<InvoiceLineItem> ImportLineItemsFromCsv(string csvPath, int invoiceId)
        {
            try
            {
                if (!File.Exists(csvPath))
                    throw new FileNotFoundException($"CSV file not found: {csvPath}");

                var lineItems = new List<InvoiceLineItem>();
                var lineNumber = 1;

                var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    TrimOptions = TrimOptions.Trim,
                    HeaderValidated = null,
                    MissingFieldFound = null
                };

                using (var reader = new StreamReader(csvPath, Encoding.UTF8))
                {
                    using (var csv = new CsvReader(reader, csvConfig))
                    {
                        csv.Read();
                        csv.ReadHeader();

                        // Find the actual Rate column name (handles "Rate (£)", "Rate", etc.)
                        var rateColumnName = csv.HeaderRecord?
                            .FirstOrDefault(h => h.Trim().StartsWith("Rate", StringComparison.OrdinalIgnoreCase))
                            ?? "Rate (£)";

                        while (csv.Read())
                        {
                            try
                            {
                                var dateStr = csv.GetField("Date") ?? "";
                                var day = csv.GetField("Day") ?? "";
                                var duration = csv.GetField("Duration") ?? "";
                                var carersStr = csv.GetField("No of Carers") ?? "1";
                                var careDescription = csv.GetField("Care Description") ?? "";
                                var rateStr = csv.GetField(rateColumnName) ?? "0";

                                // Parse date - try multiple formats
                                DateTime date;
                                if (!DateTime.TryParseExact(dateStr,
                                    new[] { "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy" },
                                    CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                                {
                                    // Fallback: try general parse
                                    if (!DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                                    {
                                        throw new Exception($"Could not parse date '{dateStr}' on row {lineNumber}");
                                    }
                                }

                                int.TryParse(carersStr, out var numberOfCarers);
                                if (numberOfCarers < 1) numberOfCarers = 1;

                                // Clean rate string: remove £ sign if present
                                rateStr = rateStr.Replace("£", "").Trim();
                                if (!decimal.TryParse(rateStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
                                {
                                    throw new Exception($"Could not parse rate '{rateStr}' on row {lineNumber}");
                                }

                                var item = new InvoiceLineItem
                                {
                                    InvoiceId = invoiceId,
                                    Date = date,
                                    Day = day.Trim(),
                                    Duration = duration.Trim(),
                                    NumberOfCarers = numberOfCarers,
                                    CareDescription = careDescription.Trim(),
                                    Rate = rate,
                                    LineNumber = lineNumber++
                                };

                                lineItems.Add(item);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error parsing CSV row {lineNumber}: {ex.Message}", ex);
                            }
                        }
                    }
                }

                return lineItems;
            }
            catch (Exception ex) when (ex.Message.StartsWith("Error parsing CSV row") || ex.Message.StartsWith("Error importing CSV"))
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing CSV file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Returns diagnostic info about CSV headers for debugging validation failures.
        /// </summary>
        public string GetHeaderDiagnostics(string csvPath)
        {
            try
            {
                using var reader = new StreamReader(csvPath, Encoding.UTF8);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Read();
                csv.ReadHeader();

                var headers = csv.HeaderRecord;
                if (headers == null || headers.Length == 0)
                    return "No headers found in CSV file.";

                var sb = new StringBuilder();
                sb.AppendLine($"Found {headers.Length} columns:");
                for (int i = 0; i < headers.Length; i++)
                {
                    var h = headers[i];
                    // Show the raw bytes to catch encoding issues
                    var bytes = Encoding.UTF8.GetBytes(h);
                    var hexBytes = string.Join(" ", bytes.Select(b => b.ToString("X2")));
                    sb.AppendLine($"  [{i}] \"{h}\" (bytes: {hexBytes})");
                }

                // Check which required headers matched/failed
                string[] coreHeaders = { "Date", "Day", "Duration", "No of Carers", "Care Description" };
                var normalizedHeaders = headers.Select(hdr => hdr.Trim()).ToArray();

                sb.AppendLine();
                sb.AppendLine("Match results:");
                foreach (var required in coreHeaders)
                {
                    bool found = normalizedHeaders.Any(h => h.Equals(required, StringComparison.OrdinalIgnoreCase));
                    sb.AppendLine($"  \"{required}\": {(found ? "FOUND" : "MISSING")}");
                }
                bool rateFound = normalizedHeaders.Any(h => h.StartsWith("Rate", StringComparison.OrdinalIgnoreCase));
                sb.AppendLine($"  \"Rate*\": {(rateFound ? "FOUND" : "MISSING")}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error reading headers: {ex.Message}";
            }
        }

        /// <summary>
        /// Validates CSV file structure - checks for required column headers.
        /// </summary>
        public bool ValidateCsvFile(string csvPath)
        {
            try
            {
                using (var reader = new StreamReader(csvPath, Encoding.UTF8))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Read();
                        csv.ReadHeader();

                        var headers = csv.HeaderRecord;
                        if (headers == null) return false;

                        // Normalize headers for comparison: trim whitespace
                        var normalizedHeaders = headers.Select(h => h.Trim()).ToArray();

                        // Check each required header, with fuzzy matching for "Rate" column
                        // (handles "Rate (£)", "Rate(£)", "Rate", "Rate (GBP)" etc.)
                        string[] coreHeaders = { "Date", "Day", "Duration", "No of Carers", "Care Description" };

                        bool coreMatch = coreHeaders.All(required =>
                            normalizedHeaders.Any(h => h.Equals(required, StringComparison.OrdinalIgnoreCase)));

                        bool rateMatch = normalizedHeaders.Any(h =>
                            h.StartsWith("Rate", StringComparison.OrdinalIgnoreCase));

                        return coreMatch && rateMatch;
                    }
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
