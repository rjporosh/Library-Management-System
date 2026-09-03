using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Logging;
using Library.Application.Common.Options;
using Library.Infrastructure.Logging;
using Library.Infrastructure.Persistence.Repositories.InMemory;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    /// <param name="settings">
    /// Bound once at startup (composition root) from the
    /// "FeatureFlags" appsettings section.
    /// </param>
    /// <param name="contentRootPath">
    /// The host's content root, used to resolve the logs folder when
    /// <see cref="ObservabilitySettings.LogsRootPath"/> is a relative
    /// path (the default).
    /// </param>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        ObservabilitySettings settings,
        string contentRootPath)
    {
        services.AddSingleton(settings);

        services.AddSingleton<IAppLogWriter>(
            new FileAppLogWriter(settings, contentRootPath));

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