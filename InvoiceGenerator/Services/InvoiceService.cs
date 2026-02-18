using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceGenerator.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceGenerator.Services
{
    public class InvoiceService
    {
        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                return await context.Invoices
                    .Include(i => i.Client)
                    .Include(i => i.LineItems)
                    .OrderByDescending(i => i.DateGenerated)
                    .ToListAsync();
            }
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                return await context.Invoices
                    .Include(i => i.Client)
                    .Include(i => i.LineItems)
                    .FirstOrDefaultAsync(i => i.Id == id);
            }
        }

        public async Task<List<Invoice>> GetInvoicesByClientAsync(int clientId)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                return await context.Invoices
                    .Include(i => i.Client)
                    .Include(i => i.LineItems)
                    .Where(i => i.ClientId == clientId)
                    .OrderByDescending(i => i.DateGenerated)
                    .ToListAsync();
            }
        }

        public async Task<Invoice> AddInvoiceAsync(Invoice invoice)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();
                return invoice;
            }
        }

        public async Task<Invoice> UpdateInvoiceAsync(Invoice invoice)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                context.Invoices.Update(invoice);
                await context.SaveChangesAsync();
                return invoice;
            }
        }

        public async Task DeleteInvoiceAsync(int id)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                var invoice = await context.Invoices.FindAsync(id);
                if (invoice != null)
                {
                    context.Invoices.Remove(invoice);
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task<List<Invoice>> SearchInvoicesAsync(string? clientName = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                var query = context.Invoices
                    .Include(i => i.Client)
                    .Include(i => i.LineItems)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(clientName))
                {
                    query = query.Where(i => i.Client!.DisplayName.Contains(clientName));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(i => i.DateGenerated >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(i => i.DateGenerated <= endDate.Value);
                }

                return await query.OrderByDescending(i => i.DateGenerated).ToListAsync();
            }
        }
    }
}
