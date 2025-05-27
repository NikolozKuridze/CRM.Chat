namespace CRM.Chat.Application.Common.Managers;

public interface IRedisManager
{
    Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);
    Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default);

    Task<bool> SetOperatorOnlineAsync(Guid operatorId, string connectionId,
        CancellationToken cancellationToken = default);

    Task<bool> SetOperatorOfflineAsync(Guid operatorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Guid>> GetOnlineOperatorsAsync(CancellationToken cancellationToken = default);
    Task<bool> IsOperatorOnlineAsync(Guid operatorId, CancellationToken cancellationToken = default);
}