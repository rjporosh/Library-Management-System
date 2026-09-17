using Library.Application.Common.Options;
using Library.Application.Common.Security;
using Library.Application.Features.Auth;
using Library.Application.Features.BookCopies;
using Library.Application.Features.Books;
using Library.Application.Features.Borrowing;
using Library.Application.Features.BulkImport;
using Library.Application.Features.BulkImport.Definitions;
using Library.Application.Features.Dashboard;
using Library.Application.Features.Members;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        BorrowingOptions? borrowing = null)
    {
        services.AddSingleton(borrowing ?? new BorrowingOptions());
        services.AddScoped<BookService>();
        services.AddScoped<BookCopyService>();
        services.AddScoped<MemberService>();
        services.AddScoped<MemberMaintenanceService>();
        services.AddScoped<BorrowingService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<AuthService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // Bulk import
        services.AddScoped<BookImportDefinition>();
        services.AddScoped<MemberImportDefinition>();
        services.AddScoped<BookCopyImportDefinition>();
        services.AddScoped<BulkImportPipeline>();
        services.AddScoped<BulkImportService>();

        return services;
    }
}
