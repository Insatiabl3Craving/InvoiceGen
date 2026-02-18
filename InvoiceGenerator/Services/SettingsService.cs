using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceGenerator.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceGenerator.Services
{
    public class SettingsService
    {
        public async Task<AppSettings> GetSettingsAsync()
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                var settings = await context.AppSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new AppSettings { Id = 1 };
                    context.AppSettings.Add(settings);
                    await context.SaveChangesAsync();
                }
                return settings;
            }
        }

        public async Task<AppSettings> UpdateSettingsAsync(AppSettings settings)
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                context.AppSettings.Update(settings);
                await context.SaveChangesAsync();
                return settings;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            using (var context = new InvoiceGeneratorDbContext())
            {
                await context.Database.EnsureCreatedAsync();
            }
        }
    }
}
