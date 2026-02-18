using System;
using System.IO;

namespace InvoiceGenerator.Services
{
    public class PdfConversionService
    {
        /// <summary>
        /// Converts a .docx file to PDF using LibreOffice
        /// This is the recommended method as it works reliably with LibreOffice installed
        /// </summary>
        public void ConvertDocxToPdf(string docxPath, string pdfPath)
        {
            try
            {
                if (!File.Exists(docxPath))
                    throw new FileNotFoundException($"DOCX file not found: {docxPath}");

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(pdfPath) ?? "";
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                // Use LibreOffice UNO API for conversion
                ConvertDocxToPdfUsingLibreOffice(docxPath, pdfPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting DOCX to PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Uses LibreOffice UNO API for conversion (requires LibreOffice installed)
        /// </summary>
        private void ConvertDocxToPdfUsingLibreOffice(string docxPath, string pdfPath)
        {
            try
            {
                if (!File.Exists(docxPath))
                    throw new FileNotFoundException($"DOCX file not found: {docxPath}");

                // Ensure output directory exists
                var outputDir = Path.GetDirectoryName(pdfPath) ?? "";
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                // Try common LibreOffice paths
                var libreOfficePaths = new[]
                {
                    @"C:\Program Files\LibreOffice\program\soffice.exe",
                    @"C:\Program Files (x86)\LibreOffice\program\soffice.exe",
                };

                string? sofficeExePath = null;
                foreach (var path in libreOfficePaths)
                {
                    if (File.Exists(path))
                    {
                        sofficeExePath = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(sofficeExePath))
                {
                    throw new FileNotFoundException("LibreOffice not found. Please ensure LibreOffice is installed at: C:\\Program Files\\LibreOffice");
                }

                var absoluteDocxPath = Path.GetFullPath(docxPath);
                var outputDirFullPath = Path.GetDirectoryName(Path.GetFullPath(pdfPath)) ?? "";

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sofficeExePath,
                    Arguments = $"--headless --convert-to pdf --outdir \"{outputDirFullPath}\" \"{absoluteDocxPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    if (process == null)
                        throw new Exception("Failed to start LibreOffice conversion process");

                    process.WaitForExit(60000); // Wait up to 60 seconds

                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        throw new Exception($"LibreOffice conversion failed with exit code {process.ExitCode}: {error}");
                    }
                }

                // Rename the output file to match expected name if needed
                var defaultNewName = Path.Combine(outputDirFullPath, Path.GetFileNameWithoutExtension(docxPath) + ".pdf");
                if (File.Exists(defaultNewName) && defaultNewName != pdfPath)
                {
                    File.Move(defaultNewName, pdfPath, true);
                }
                else if (!File.Exists(pdfPath) && File.Exists(defaultNewName))
                {
                    File.Move(defaultNewName, pdfPath);
                }

                if (!File.Exists(pdfPath))
                    throw new Exception("PDF file was not created. Check LibreOffice output.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error converting DOCX to PDF using LibreOffice: {ex.Message}", ex);
            }
        }
    }
}
