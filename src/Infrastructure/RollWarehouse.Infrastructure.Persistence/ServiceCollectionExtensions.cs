using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RollWarehouse.Application.Abstractions.Ports;
using RollWarehouse.Infrastructure.Persistence.Repositories;

namespace RollWarehouse.Infrastructure.Persistence
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration config)
        {
            var conn = Environment.GetEnvironmentVariable("CONN_STR")
                       ?? config.GetConnectionString("Default");

            services.AddDbContext<PersistenceContext>(opt =>
                opt.UseNpgsql(conn));

            services.AddScoped<IRollRepository, EfRollRepository>();

            return services;
        }
    }
}
