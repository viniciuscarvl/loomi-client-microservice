using Azure.Storage.Blobs;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ClientMicroservice.Application.Common.Interfaces;
using ClientMicroservice.Domain.Abstractions;
using ClientMicroservice.Infrastructure.Caching;
using ClientMicroservice.Infrastructure.Messaging;
using ClientMicroservice.Infrastructure.Messaging.Consumers;
using ClientMicroservice.Infrastructure.Persistence;
using ClientMicroservice.Infrastructure.Persistence.Repositories;
using ClientMicroservice.Infrastructure.Storage;

namespace ClientMicroservice.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventBus, MassTransitEventBus>();

        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = configuration["Redis:ConnectionString"]
                ?? throw new InvalidOperationException("Redis:ConnectionString is required"));
        services.AddScoped<ICacheService, RedisCacheService>();

        var blobConnectionString = configuration["Azure:BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("Azure:BlobStorage:ConnectionString is required");
        var containerName = configuration["Azure:BlobStorage:ContainerName"]
            ?? throw new InvalidOperationException("Azure:BlobStorage:ContainerName is required");
        services.AddSingleton(new BlobServiceClient(blobConnectionString));
        services.AddScoped<IStorageService>(sp =>
            new AzureBlobStorageService(sp.GetRequiredService<BlobServiceClient>(), containerName));

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<ClientCreatedEventConsumer>();

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
