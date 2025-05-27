namespace CRM.Chat.Application.Common.Abstractions.Mediators;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
}