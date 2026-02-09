using Microsoft.Extensions.DependencyInjection;
using RollWarehouse.Application.Services;

namespace RollWarehouse.Application
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<RollService>();
            return services;
        }
    }
}
