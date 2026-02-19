using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace InvoiceGenerator.Services
{
    public class WordTemplateService
    {
        /// <summary>
        /// Replaces placeholders in Word document with provided values
        /// Placeholders should be in format {{PLACEHOLDER_NAME}}
        /// </summary>
        public void ReplaceTemplateFields(string templatePath, string outputPath, Dictionary<string, string> replacements)
        {
            try
            {
                // Copy template to output location
                File.Copy(templatePath, outputPath, true);

                // Open the document
                using (var doc = WordprocessingDocument.Open(outputPath, true))
                {
                    var mainPart = doc.MainDocumentPart;
                    if (mainPart == null) throw new InvalidOperationException("Could not access main document part");

                    // Replace in main body
                    var body = mainPart.Document.Body;
                    if (body != null)
                    {
                        ReplaceTextInElement(body, replacements);
                    }

                    // Replace in headers/footers
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

        private void ReplaceTextInElement(OpenXmlElement element, Dictionary<string, string> replacements)
        {
            // Process each paragraph so that placeholders split across multiple runs are handled
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
            allTexts[0].Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve;
            for (int i = 1; i < allTexts.Count; i++)
            {
                allTexts[i].Text = string.Empty;
            }
        }
    }
}
