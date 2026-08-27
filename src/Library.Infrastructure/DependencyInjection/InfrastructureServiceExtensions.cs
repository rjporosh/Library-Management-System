using Library.Application.Abstractions.Persistence;
using Library.Infrastructure.Persistence.Repositories.InMemory;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        // Register concrete repositories.
        // Seeder needs concrete types because Seed() is
        // intentionally an infrastructure-specific operation.
        services.AddSingleton<InMemoryBookRepository>();
        services.AddSingleton<InMemoryBookCopyRepository>();
        services.AddSingleton<InMemoryMemberRepository>();
        services.AddSingleton<InMemoryBorrowRecordRepository>();

        // Expose the same singleton instances through
        // Application-layer persistence abstractions.
        services.AddSingleton<IBookRepository>(sp =>
            sp.GetRequiredService<InMemoryBookRepository>());

        services.AddSingleton<IBookCopyRepository>(sp =>
            sp.GetRequiredService<InMemoryBookCopyRepository>());

        services.AddSingleton<IMemberRepository>(sp =>
            sp.GetRequiredService<InMemoryMemberRepository>());

        services.AddSingleton<IBorrowRecordRepository>(sp =>
            sp.GetRequiredService<InMemoryBorrowRecordRepository>());

        // Seed initial in-memory data during application startup.
        services.AddSingleton<InMemoryDataSeeder>();

        return services;
    }
}