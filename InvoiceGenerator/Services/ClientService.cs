using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceGenerator.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceGenerator.Services
{
    public class ClientService
    {
        public async Task<List<Client>> GetAllClientsAsync()
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                return await context.Clients.OrderBy(c => c.DisplayName).ToListAsync();
            }
        }

        public async Task<Client?> GetClientByIdAsync(int id)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                return await context.Clients.FirstOrDefaultAsync(c => c.Id == id);
            }
        }

        public async Task<Client> AddClientAsync(Client client)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                context.Clients.Add(client);
                await context.SaveChangesAsync();
                return client;
            }
        }

        public async Task<Client> UpdateClientAsync(Client client)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                client.ModifiedDate = DateTime.Now;
                context.Clients.Update(client);
                await context.SaveChangesAsync();
                return client;
            }
        }

        public async Task DeleteClientAsync(int id)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                var client = await context.Clients.FindAsync(id);
                if (client != null)
                {
                    context.Clients.Remove(client);
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
