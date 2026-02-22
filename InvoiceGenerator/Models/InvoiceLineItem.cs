using System;

namespace InvoiceGenerator.Models
{
    public class InvoiceLineItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        /// <summary>Date of the care visit</summary>
        public DateTime Date { get; set; }

        /// <summary>Day of the week (e.g. "Monday")</summary>
        public string Day { get; set; } = string.Empty;

        /// <summary>Duration of visit (e.g. "30 minutes", "45 minutes")</summary>
        public string Duration { get; set; } = string.Empty;

        /// <summary>Number of carers for this visit</summary>
        public int NumberOfCarers { get; set; }

        /// <summary>Description of care provided</summary>
        public string CareDescription { get; set; } = string.Empty;

        /// <summary>Flat rate for this visit in £</summary>
        public decimal Rate { get; set; }

        /// <summary>1-based line ordering</summary>
        public int LineNumber { get; set; }
    }
}
