using System;
using System.Collections.Generic;

namespace InvoiceGenerator.Models
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public Client? Client { get; set; }
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public DateTime DateGenerated { get; set; } = DateTime.Now;
        public string DocxFilePath { get; set; } = string.Empty;
        public string PdfFilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Generated"; // Generated, Sent, etc.
        public DateTime? EmailSentDate { get; set; }
        public List<InvoiceLineItem>? LineItems { get; set; }
    }
}
