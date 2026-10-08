using Library.Application.Features.BorrowRequests.Models;

namespace Library.Application.Abstractions;

public sealed record LibraryNotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    DateTime Timestamp,
    Guid? RequestId = null,
    Guid? BookId = null,
    string? BookTitle = null,
    string? MemberName = null,
    string? MembershipNumber = null,
    string? Status = null,
    string? SuggestedAuthor = null,
    string? Note = null);

public interface ILibraryNotificationPublisher
{
    Task PublishBorrowRequestAsync(BorrowRequestResponse request, CancellationToken cancellationToken = default);
    Task PublishLowStockAlertAsync(string bookTitle, int availableCopies, int totalCopies, CancellationToken cancellationToken = default);
}

