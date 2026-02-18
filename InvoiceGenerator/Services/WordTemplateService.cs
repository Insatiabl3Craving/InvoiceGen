using System;
using System.Collections.Generic;
using System.IO;
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
            foreach (var run in element.Descendants<Run>())
            {
                foreach (var text in run.Descendants<Text>())
                {
                    foreach (var kvp in replacements)
                    {
                        // Replace placeholder with the value
                        var pattern = $@"{{\{{{Regex.Escape(kvp.Key)}\}}}}";
                        if (Regex.IsMatch(text.Text, pattern, RegexOptions.IgnoreCase))
                        {
                            text.Text = Regex.Replace(text.Text, pattern, kvp.Value, RegexOptions.IgnoreCase);
                        }
                    }
                }
            }
        }
    }
}
