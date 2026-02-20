using Microsoft.EntityFrameworkCore;

namespace InvoiceGenerator.Models
{
    public class InvoiceGeneratorDbContext : DbContext
    {
        public DbSet<Client> Clients { get; set; } = null!;
        public DbSet<Invoice> Invoices { get; set; } = null!;
        public DbSet<InvoiceLineItem> InvoiceLineItems { get; set; } = null!;
        public DbSet<AppSettings> AppSettings { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string dbPath = System.IO.Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                "InvoiceGenerator",
                "InvoiceGenerator.db"
            );

            // Ensure directory exists
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath) ?? "");

            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Client)
                .WithMany()
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InvoiceLineItem>()
                .HasOne(il => il.Invoice)
                .WithMany(i => i.LineItems)
                .HasForeignKey(il => il.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
