using Library.Application.Features.Books;
using Library.Application.Features.Borrowing;
using Library.Application.Features.Members;
using Microsoft.Extensions.DependencyInjection;

namespace Library.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<BookService>();
        services.AddScoped<MemberService>();
        services.AddScoped<BorrowingService>();

        return services;
    }
}
