using CRM.Chat.Domain.Common.Models;

namespace CRM.Chat.Application.Common.Abstractions.Mediators;

public interface IMediator
{
    ValueTask<Result<TResponse>> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}