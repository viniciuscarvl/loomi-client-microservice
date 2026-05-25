using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Infrastructure.Messaging;
using ClientMicroservice.Infrastructure.Messaging.Consumers;
using ClientMicroservice.Infrastructure.Persistence;
using ClientMicroservice.Infrastructure.Persistence.Repositories;

namespace ClientMicroservice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<UserCreatedEventConsumer>();

            bus.UsingRabbitMq((ctx, cfg) =>
            {
                cfg.Host(configuration["RabbitMq:Host"], h =>
                {
                    h.Username(configuration["RabbitMq:Username"]!);
                    h.Password(configuration["RabbitMq:Password"]!);
                });

                cfg.ConfigureEndpoints(ctx);
            });
        });

        return services;
    }
}
