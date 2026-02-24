using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using InvoiceGenerator.Models;

namespace InvoiceGenerator.Services
{
    public class WordTemplateService
    {
        // Placeholders that identify a line-item template row
        private static readonly string[] LineItemPlaceholders = {
            "{{DATE}}", "{{DAY}}", "{{DURATION}}", "{{NO_OF_CARERS}}", "{{CARE_DESCRIPTION}}", "{{RATE}}"
        };

        /// <summary>
        /// Generates an invoice document from a Word template.
        /// Replaces header-level placeholders and populates the line-items table
        /// by cloning the template row for each line item.
        /// </summary>
        public void GenerateInvoiceFromTemplate(
            string templatePath,
            string outputPath,
            Dictionary<string, string> headerReplacements,
            List<InvoiceLineItem> lineItems)
        {
            try
            {
                // Copy template to output location
                File.Copy(templatePath, outputPath, true);

                using (var doc = WordprocessingDocument.Open(outputPath, true))
                {
                    var mainPart = doc.MainDocumentPart;
                    if (mainPart == null) throw new InvalidOperationException("Could not access main document part");

                    var body = mainPart.Document.Body;
                    if (body == null) throw new InvalidOperationException("Could not access document body");

                    // Step 1: Find and populate the line-items table
                    PopulateLineItemsTable(body, lineItems);

                    // Step 2: Replace all header-level placeholders in body, headers, footers
                    ReplaceTextInElement(body, headerReplacements);

                    foreach (var headerPart in mainPart.HeaderParts)
                    {
                        ReplaceTextInElement(headerPart.Header, headerReplacements);
                    }

                    foreach (var footerPart in mainPart.FooterParts)
                    {
                        ReplaceTextInElement(footerPart.Footer, headerReplacements);
                    }

                    mainPart.Document.Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating invoice from template: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Simple placeholder replacement (legacy method kept for backward compatibility).
        /// </summary>
        public void ReplaceTemplateFields(string templatePath, string outputPath, Dictionary<string, string> replacements)
        {
            try
            {
                File.Copy(templatePath, outputPath, true);

                using (var doc = WordprocessingDocument.Open(outputPath, true))
                {
                    var mainPart = doc.MainDocumentPart;
                    if (mainPart == null) throw new InvalidOperationException("Could not access main document part");

                    var body = mainPart.Document.Body;
                    if (body != null)
                    {
                        ReplaceTextInElement(body, replacements);
                    }

                    foreach (var headerPart in mainPart.HeaderParts)
                    {
                        ReplaceTextInElement(headerPart.Header, replacements);
                    }

                    foreach (var footerPart in mainPart.FooterParts)
                    {
                        ReplaceTextInElement(footerPart.Footer, replacements);
                    }

                    mainPart.Document.Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error replacing template fields: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Finds the template row in a table (a row containing line-item placeholders),
        /// clones it for each line item, fills in the values, and removes the original template row.
        /// </summary>
        private void PopulateLineItemsTable(Body body, List<InvoiceLineItem> lineItems)
        {
            // Find all tables in the document
            var tables = body.Descendants<Table>().ToList();

            foreach (var table in tables)
            {
                // Look for a row that contains line-item placeholders
                TableRow? templateRow = null;

                foreach (var row in table.Elements<TableRow>())
                {
                    var rowText = GetFullRowText(row);
                    if (LineItemPlaceholders.Any(p => rowText.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    {
                        templateRow = row;
                        break;
                    }
                }

                if (templateRow == null)
                    continue; // This table doesn't contain line-item placeholders

                // Clone the template row for each line item and insert before the template row
                foreach (var item in lineItems)
                {
                    var newRow = (TableRow)templateRow.CloneNode(true);

                    // Build per-row replacements
                    var rowReplacements = new Dictionary<string, string>
                    {
                        { "DATE", item.Date.ToString("dd/MM/yyyy") },
                        { "DAY", item.Day },
                        { "DURATION", item.Duration },
                        { "NO_OF_CARERS", item.NumberOfCarers.ToString() },
                        { "CARE_DESCRIPTION", item.CareDescription },
                        { "RATE", item.Rate.ToString("F2") }
                    };

                    // Replace placeholders in each cell of the cloned row
                    foreach (var paragraph in newRow.Descendants<Paragraph>())
                    {
                        ReplacePlaceholdersInParagraph(paragraph, rowReplacements);
                    }

                    // Insert the populated row before the template row
                    table.InsertBefore(newRow, templateRow);
                }

                // Remove the original template row
                templateRow.Remove();

                // We found and populated the table — done
                return;
            }
        }

        /// <summary>
        /// Gets the concatenated text content of all cells in a table row.
        /// Handles cases where Word splits placeholders across multiple XML runs.
        /// </summary>
        private static string GetFullRowText(TableRow row)
        {
            return string.Concat(row.Descendants<Text>().Select(t => t.Text));
        }

        private void ReplaceTextInElement(OpenXmlElement element, Dictionary<string, string> replacements)
        {
            foreach (var paragraph in element.Descendants<Paragraph>())
            {
                ReplacePlaceholdersInParagraph(paragraph, replacements);
            }
        }

        private static void ReplacePlaceholdersInParagraph(Paragraph paragraph, Dictionary<string, string> replacements)
        {
            var texts = paragraph.Descendants<Text>().ToList();
            if (texts.Count == 0) return;

            // First pass: simple per-Text-node replacement (covers the common case)
            foreach (var text in texts)
            {
                foreach (var kvp in replacements)
                {
                    var pattern = $@"{{\{{{Regex.Escape(kvp.Key)}\}}}}";
                    if (Regex.IsMatch(text.Text, pattern, RegexOptions.IgnoreCase))
                    {
                        text.Text = Regex.Replace(text.Text, pattern, kvp.Value, RegexOptions.IgnoreCase);
                    }
                }
            }

            // Second pass: if any placeholder still spans multiple runs, consolidate paragraph text
            var fullText = string.Concat(paragraph.Descendants<Text>().Select(t => t.Text));
            bool stillHasPlaceholder = false;
            foreach (var kvp in replacements)
            {
                var pattern = $@"{{\{{{Regex.Escape(kvp.Key)}\}}}}";
                if (Regex.IsMatch(fullText, pattern, RegexOptions.IgnoreCase))
                {
                    stillHasPlaceholder = true;
                    break;
                }
            }

            if (!stillHasPlaceholder) return;

            // Merge all text into the first Text node, clear the rest, then do replacement
            var allTexts = paragraph.Descendants<Text>().ToList();
            if (allTexts.Count == 0) return;

            var mergedText = string.Concat(allTexts.Select(t => t.Text));
            foreach (var kvp in replacements)
            {
                var pattern = $@"{{\{{{Regex.Escape(kvp.Key)}\}}}}";
                mergedText = Regex.Replace(mergedText, pattern, kvp.Value, RegexOptions.IgnoreCase);
            }

            allTexts[0].Text = mergedText;
            allTexts[0].Space = SpaceProcessingModeValues.Preserve;
            for (int i = 1; i < allTexts.Count; i++)
            {
                allTexts[i].Text = string.Empty;
            }
        }
    }
}
