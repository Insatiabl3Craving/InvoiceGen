namespace InvoiceGenerator.Models
{
    public class AppSettings
    {
        public int Id { get; set; }
        public string TemplateFilePath { get; set; } = string.Empty;
        public string InvoicesFolderPath { get; set; } = string.Empty;
        public string EmailSmtpServer { get; set; } = string.Empty;
        public int EmailSmtpPort { get; set; } = 587;
        public string EmailFromAddress { get; set; } = string.Empty;
        public string EmailDefaultSubject { get; set; } = "Invoice {InvoiceNumber} - {DateRange}";
        public string EmailDefaultBody { get; set; } = "Please find attached your invoice.";
        public string CsvColumnMapping { get; set; } = "{}"; // JSON format for column mappings
        public bool EmailUseTls { get; set; } = true;
    }
}
