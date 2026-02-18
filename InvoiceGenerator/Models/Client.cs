using System;

namespace InvoiceGenerator.Models
{
    public class Client
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string BillingAddress { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string? AdditionalInfo { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }
}
