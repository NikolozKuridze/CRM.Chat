using CRM.Chat.Application.Common.Abstractions.Mediators;
using CRM.Chat.Domain.Common.Models;

namespace CRM.Chat.Application.Common.Services;

public interface IChatAssignmentService
{
    Task<Result<Guid>> AssignChatToOperatorAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<Result<Guid>> ReassignChatAsync(Guid chatId, CancellationToken cancellationToken = default);

    Task<Result<Guid>> TransferChatAsync(Guid chatId, Guid newOperatorId, string reason,
        CancellationToken cancellationToken = default);

    Task<Result<Unit>> ReleaseChatFromOperatorAsync(Guid chatId, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Guid>>> GetInactiveChatsForReassignmentAsync(CancellationToken cancellationToken = default);
}