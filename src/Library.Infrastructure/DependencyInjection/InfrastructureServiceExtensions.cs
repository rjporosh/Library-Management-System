using Library.Application.Abstractions;
using Library.Application.Abstractions.Persistence;
using Library.Application.Common.Logging;
using Library.Application.Common.Options;
using Library.Application.Features.BulkImport;
using Library.Application.Features.Dashboard;
using Library.Infrastructure.BulkImport;
using Library.Infrastructure.Logging;
using Library.Infrastructure.Persistence;
using Library.Infrastructure.Persistence.Dapper;
using Library.Infrastructure.Persistence.Interceptors;
using Library.Infrastructure.Persistence.Repositories.EfCore;
using Library.Infrastructure.Persistence.Repositories.InMemory;
using Library.Infrastructure.Persistence.Repositories.InMemory.Seed;
using Library.Infrastructure.Persistence.Seed;
using Library.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        ObservabilitySettings settings,
        DatabaseOptions database,
        string contentRootPath,
        JwtOptions? jwt = null)
    {
        services.AddSingleton(settings);
        services.AddSingleton(database);
        services.AddSingleton(jwt ?? new JwtOptions());
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddSingleton<IAppLogWriter>(new FileAppLogWriter(settings, contentRootPath));

        services.AddSingleton(new BulkImportOptions());
        services.AddSingleton<IWorkbookReader, ClosedXmlWorkbookReader>();
        services.AddSingleton<IImportTemplateWriter, ClosedXmlTemplateWriter>();

        if (database.IsRelational)
        {
            AddEfCore(services, database);
        }
        else
        {
            AddInMemory(services);
        }

        return services;
    }

    private static void AddEfCore(IServiceCollection services, DatabaseOptions database)
    {
        services.AddSingleton<QueryLoggingInterceptor>();

        services.AddDbContext<LibraryDbContext>((sp, options) =>
        {
            DatabaseProviderConfigurator.Configure(options, database);
            options.AddInterceptors(sp.GetRequiredService<QueryLoggingInterceptor>());
        });

        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IBookRepository, EfBookRepository>();
        services.AddScoped<IBookCopyRepository, EfBookCopyRepository>();
        services.AddScoped<IMemberRepository, EfMemberRepository>();
        services.AddScoped<IBorrowRecordRepository, EfBorrowRecordRepository>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<DatabaseSeeder>();

        // Read-path ORM toggle: writes and advanced search always use EF Core;
        // the dashboard aggregate can use Dapper (one round-trip of SQL COUNTs).
        var dapperReads = database.UseDapperReads
            && database.ResolvedProvider is DatabaseProvider.Postgres or DatabaseProvider.Sqlite;

        if (dapperReads)
        {
            services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
            services.AddScoped<IDashboardReadStore, DapperDashboardReadStore>();
        }
        else
        {
            services.AddScoped<IDashboardReadStore, EfDashboardReadStore>();
        }
    }

    private static void AddInMemory(IServiceCollection services)
    {
        services.AddSingleton<InMemoryBookRepository>();
        services.AddSingleton<InMemoryBookCopyRepository>();
        services.AddSingleton<InMemoryMemberRepository>();
        services.AddSingleton<InMemoryBorrowRecordRepository>();
        services.AddSingleton<InMemoryUserRepository>();

        services.AddSingleton<IBookRepository>(sp => sp.GetRequiredService<InMemoryBookRepository>());
        services.AddSingleton<IBookCopyRepository>(sp => sp.GetRequiredService<InMemoryBookCopyRepository>());
        services.AddSingleton<IMemberRepository>(sp => sp.GetRequiredService<InMemoryMemberRepository>());
        services.AddSingleton<IBorrowRecordRepository>(sp => sp.GetRequiredService<InMemoryBorrowRecordRepository>());
        services.AddSingleton<IUserRepository>(sp => sp.GetRequiredService<InMemoryUserRepository>());
        services.AddSingleton<IUnitOfWork, NoOpUnitOfWork>();
        services.AddScoped<IDashboardReadStore, InMemoryDashboardReadStore>();

        services.AddSingleton<InMemoryDataSeeder>();
    }
}
