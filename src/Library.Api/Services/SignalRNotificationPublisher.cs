using Library.Api.Hubs;
using Library.Application.Abstractions;
using Library.Application.Features.BorrowRequests.Models;
using Microsoft.AspNetCore.SignalR;

namespace Library.Api.Services;

public sealed class SignalRNotificationPublisher(IHubContext<NotificationHub> hubContext) : ILibraryNotificationPublisher
{
    public async Task PublishBorrowRequestAsync(BorrowRequestResponse request, CancellationToken cancellationToken = default)
    {
        var isBorrow = request.Type == Domain.Enums.BorrowRequestType.Borrow;
        var notification = new LibraryNotificationDto(
            Id: Guid.NewGuid(),
            Type: request.Type.ToString(),
            Title: isBorrow
                ? $"New Borrow Request: {request.BookTitle}"
                : $"New Purchase Suggestion: {request.SuggestedTitle}",
            Message: isBorrow
                ? $"{request.MemberName} ({request.MembershipNumber}) requested to borrow \"{request.BookTitle}\"."
                : $"{request.MemberName} ({request.MembershipNumber}) suggested acquiring \"{request.SuggestedTitle}\"{(string.IsNullOrWhiteSpace(request.SuggestedAuthor) ? "" : $" by {request.SuggestedAuthor}")}.",
            Timestamp: DateTime.UtcNow,
            RequestId: request.Id,
            BookId: request.BookId,
            BookTitle: request.BookTitle ?? request.SuggestedTitle,
            MemberName: request.MemberName,
            MembershipNumber: request.MembershipNumber,
            Status: request.Status.ToString(),
            SuggestedAuthor: request.SuggestedAuthor,
            Note: request.Note);

        await hubContext.Clients.All.SendAsync(NotificationHub.ReceiveBorrowRequestMethod, notification, cancellationToken);
        await hubContext.Clients.All.SendAsync(NotificationHub.ReceiveNotificationMethod, notification, cancellationToken);
    }

    public async Task PublishLowStockAlertAsync(string bookTitle, int availableCopies, int totalCopies, CancellationToken cancellationToken = default)
    {
        var notification = new LibraryNotificationDto(
            Id: Guid.NewGuid(),
            Type: "LowStock",
            Title: "Low Stock Alert: Buy More Copies",
            Message: $"Book \"{bookTitle}\" has only {availableCopies} copy available out of {totalCopies}! Recommendation: Buy more copies.",
            Timestamp: DateTime.UtcNow,
            BookTitle: bookTitle,
            Status: "LowStock");

        await hubContext.Clients.All.SendAsync(NotificationHub.ReceiveNotificationMethod, notification, cancellationToken);
    }
}

