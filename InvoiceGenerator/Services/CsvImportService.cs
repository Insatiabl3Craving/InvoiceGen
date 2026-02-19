using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CsvHelper;
using InvoiceGenerator.Models;

namespace InvoiceGenerator.Services
{
    public class CsvImportService
    {
        /// <summary>
        /// Imports invoice line items from a CSV file
        /// Expected columns: Description, Quantity, UnitRate (will calculate Amount)
        /// </summary>
        public List<InvoiceLineItem> ImportLineItemsFromCsv(string csvPath, int invoiceId)
        {
            try
            {
                if (!File.Exists(csvPath))
                    throw new FileNotFoundException($"CSV file not found: {csvPath}");

                var lineItems = new List<InvoiceLineItem>();
                var lineNumber = 1;

                using (var reader = new StreamReader(csvPath, Encoding.UTF8))
                {
                    using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                    {
                        csv.Read();
                        csv.ReadHeader();

                        while (csv.Read())
                        {
                            try
                            {
                                var description = csv.GetField("Description") ?? "";
                                var quantityStr = csv.GetField("Quantity") ?? "0";
                                var unitRateStr = csv.GetField("UnitRate") ?? "0";

                                if (decimal.TryParse(quantityStr, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity) &&
                                    decimal.TryParse(unitRateStr, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var unitRate))
                                {
                                    var amount = quantity * unitRate;

                                    var item = new InvoiceLineItem
                                    {
                                        InvoiceId = invoiceId,
                                        Description = description,
                                        Quantity = quantity,
                                        UnitRate = unitRate,
                                        Amount = amount,
                                        LineNumber = lineNumber++
                                    };

                                    lineItems.Add(item);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw new Exception($"Error parsing CSV row: {ex.Message}", ex);
                            }
                        }
                    }
                }

                return lineItems;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error importing CSV file: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates CSV file structure
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
                        var requiredHeaders = new[] { "Description", "Quantity", "UnitRate" };

                        return requiredHeaders.All(h => headers?.Contains(h) ?? false);
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
