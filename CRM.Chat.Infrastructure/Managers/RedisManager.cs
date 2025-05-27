using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CRM.Chat.Infrastructure.Managers;

public class RedisManager : IRedisManager
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisManager> _logger;

    private const string OnlineOperatorsPrefix = "chat:operators:online";
    private const string OperatorConnectionPrefix = "chat:operator:connection:";
    private const string OperatorStatusPrefix = "chat:operator:status:";
    private const string ChatActivityPrefix = "chat:activity:";

    public RedisManager(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisManager> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _logger = logger;
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var serializedValue = JsonSerializer.Serialize(value);
            return await db.StringSetAsync(key, serializedValue, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in Redis for key {Key}", key);
            return false;
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from Redis for key {Key}", key);
            return default;
        }
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from Redis", key);
            return false;
        }
    }

    public async Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key {Key} exists in Redis", key);
            return false;
        }
    }

    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.StringIncrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing key {Key} in Redis", key);
            return 0;
        }
    }

    public async Task<long> DecrementAsync(string key, long value = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.StringDecrementAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrementing key {Key} in Redis", key);
            return 0;
        }
    }

    public async Task<bool> SetOperatorOnlineAsync(Guid operatorId, string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();

            // Add to online operators set
            await db.SetAddAsync(OnlineOperatorsPrefix, operatorId.ToString());

            // Store connection ID
            var connectionKey = $"{OperatorConnectionPrefix}{operatorId}";
            await db.StringSetAsync(connectionKey, connectionId, TimeSpan.FromHours(24));

            // Set status
            var statusKey = $"{OperatorStatusPrefix}{operatorId}";
            await db.StringSetAsync(statusKey, "Online", TimeSpan.FromHours(24));

            _logger.LogInformation("Operator {OperatorId} set online with connection {ConnectionId}", operatorId,
                connectionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting operator {OperatorId} online", operatorId);
            return false;
        }
    }

    public async Task<bool> SetOperatorOfflineAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();

            // Remove from online operators set
            await db.SetRemoveAsync(OnlineOperatorsPrefix, operatorId.ToString());

            // Remove connection
            var connectionKey = $"{OperatorConnectionPrefix}{operatorId}";
            await db.KeyDeleteAsync(connectionKey);

            // Update status
            var statusKey = $"{OperatorStatusPrefix}{operatorId}";
            await db.StringSetAsync(statusKey, "Offline", TimeSpan.FromHours(1));

            _logger.LogInformation("Operator {OperatorId} set offline", operatorId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting operator {OperatorId} offline", operatorId);
            return false;
        }
    }

    public async Task<IEnumerable<Guid>> GetOnlineOperatorsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            var operators = await db.SetMembersAsync(OnlineOperatorsPrefix);

            return operators
                .Where(op => Guid.TryParse(op, out _))
                .Select(op => Guid.Parse(op!))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting online operators from Redis");
            return Enumerable.Empty<Guid>();
        }
    }

    public async Task<bool> IsOperatorOnlineAsync(Guid operatorId, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _connectionMultiplexer.GetDatabase();
            return await db.SetContainsAsync(OnlineOperatorsPrefix, operatorId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if operator {OperatorId} is online", operatorId);
            return false;
        }
    }
}