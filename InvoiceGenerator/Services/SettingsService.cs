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
                await EnsureAppSettingsAuthColumnsAsync(context);
            }
        }

        private static async Task EnsureAppSettingsAuthColumnsAsync(InvoiceGeneratorDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            await connection.OpenAsync();

            try
            {
                var existingColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA table_info(AppSettings);";
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        existingColumns.Add(reader.GetString(1));
                    }
                }

                var alterStatements = new List<string>();

                if (!existingColumns.Contains("AppPasswordHash"))
                {
                    alterStatements.Add("ALTER TABLE AppSettings ADD COLUMN AppPasswordHash TEXT NULL;");
                }

                if (!existingColumns.Contains("AppPasswordSalt"))
                {
                    alterStatements.Add("ALTER TABLE AppSettings ADD COLUMN AppPasswordSalt TEXT NULL;");
                }

                if (!existingColumns.Contains("AppPasswordIterations"))
                {
                    alterStatements.Add("ALTER TABLE AppSettings ADD COLUMN AppPasswordIterations INTEGER NOT NULL DEFAULT 0;");
                }

                if (!existingColumns.Contains("AppPasswordCreatedUtc"))
                {
                    alterStatements.Add("ALTER TABLE AppSettings ADD COLUMN AppPasswordCreatedUtc TEXT NULL;");
                }

                foreach (var sql in alterStatements)
                {
                    using var alterCommand = connection.CreateCommand();
                    alterCommand.CommandText = sql;
                    await alterCommand.ExecuteNonQueryAsync();
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
    }
}
