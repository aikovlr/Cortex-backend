using Cortex.Infra.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cortex.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => 
                    d.ServiceType == typeof(DbContextOptions<CortexDbContext>) ||
                    d.ServiceType == typeof(DbContextOptions)).ToList();
                foreach (var d in descriptors)
                {
                    services.Remove(d);
                }

                var internalServiceProvider = new ServiceCollection()
                    .AddEntityFrameworkInMemoryDatabase()
                    .BuildServiceProvider();

                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<CortexDbContext>(options =>
                {
                    options.UseInMemoryDatabase(dbName)
                           .UseInternalServiceProvider(internalServiceProvider);
                });

                var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
                foreach (var hs in hostedServices)
                {
                    services.Remove(hs);
                }
            });

            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "CortexBackendSuperSecretSecurityKey_Min32CharsLong2026!"
                });
            });
        }
    }
}
