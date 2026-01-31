using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Common
{
    public static class MigrationRunner
    {
        public static async Task MigrateDbContextAsync<Tcontext>(this IHost host) where Tcontext : DbContext
        {
           using var scope = host.Services.CreateScope();
           var services = scope.ServiceProvider;
           var loggerFactory = services.GetRequiredService<ILoggerFactory>();
           var logger = loggerFactory.CreateLogger(typeof(MigrationRunner));
            try
            {
                var context = services.GetRequiredService<Tcontext>();
                await context.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occured while migrating or seeding the database.");
            }

            logger.LogInformation("Database migration ran successfully");
        }
    }
}
