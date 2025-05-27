using CRM.Chat.Application.Common.Specifications.Operators;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Infrastructure.Services;

public class LoadBalancingService : ILoadBalancingService
{
    private readonly IRepository<ChatOperator> _operatorRepository;
    private readonly IRedisManager _redisManager;
    private readonly ILogger<LoadBalancingService> _logger;

    public LoadBalancingService(
        IRepository<ChatOperator> operatorRepository,
        IRedisManager redisManager,
        ILogger<LoadBalancingService> logger)
    {
        _operatorRepository = operatorRepository;
        _redisManager = redisManager;
        _logger = logger;
    }

    public async Task<Result<Guid>> FindBestAvailableOperatorAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Get online operators from Redis
            var onlineOperatorIds = await _redisManager.GetOnlineOperatorsAsync(cancellationToken);

            if (!onlineOperatorIds.Any())
            {
                _logger.LogWarning("No online operators found in Redis cache");
                return Result.Failure<Guid>("No online operators available", "NotFound");
            }

            // Get available operators from database
            var availableOperatorsSpec = new AvailableOperatorsSpec();
            var availableOperators = await _operatorRepository.ListAsync(availableOperatorsSpec, cancellationToken);

            // Filter to only include operators that are both online (in Redis) and available (in DB)
            var eligibleOperators = availableOperators
                .Where(op => onlineOperatorIds.Contains(op.UserId))
                .ToList();

            if (!eligibleOperators.Any())
            {
                _logger.LogWarning("No eligible operators found for chat assignment");
                return Result.Failure<Guid>("No available operators found", "NotFound");
            }

            // Find operator with lowest workload (least busy)
            var bestOperator = eligibleOperators
                .OrderBy(op => op.GetWorkloadPercentage())
                .ThenBy(op => op.CurrentChatCount)
                .ThenBy(op => op.LastActiveAt) // Prefer recently active operators
                .First();

            _logger.LogInformation(
                "Selected operator {OperatorId} with workload {Workload}% ({CurrentChats}/{MaxChats})",
                bestOperator.UserId,
                bestOperator.GetWorkloadPercentage(),
                bestOperator.CurrentChatCount,
                bestOperator.MaxConcurrentChats);

            return Result.Success(bestOperator.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding best available operator");
            return Result.Failure<Guid>("Failed to find available operator", "InternalServerError");
        }
    }

    public async Task<Result<IEnumerable<Guid>>> GetAvailableOperatorsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onlineOperatorIds = await _redisManager.GetOnlineOperatorsAsync(cancellationToken);
            var availableOperatorsSpec = new AvailableOperatorsSpec();
            var availableOperators = await _operatorRepository.ListAsync(availableOperatorsSpec, cancellationToken);

            var eligibleOperatorIds = availableOperators
                .Where(op => onlineOperatorIds.Contains(op.UserId))
                .Select(op => op.UserId)
                .ToList();

            _logger.LogInformation("Found {Count} available operators", eligibleOperatorIds.Count);

            return Result.Success<IEnumerable<Guid>>(eligibleOperatorIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available operators");
            return Result.Failure<IEnumerable<Guid>>("Failed to get available operators", "InternalServerError");
        }
    }

    public async Task<Result<double>> GetOperatorWorkloadAsync(Guid operatorId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var operatorSpec = new OperatorByUserIdSpec(operatorId);
            var chatOperator = await _operatorRepository.FirstOrDefaultAsync(operatorSpec, cancellationToken);

            if (chatOperator == null)
            {
                return Result.Failure<double>("Operator not found", "NotFound");
            }

            var workload = chatOperator.GetWorkloadPercentage();

            _logger.LogDebug("Operator {OperatorId} workload: {Workload}%", operatorId, workload);

            return Result.Success(workload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting workload for operator {OperatorId}", operatorId);
            return Result.Failure<double>("Failed to get operator workload", "InternalServerError");
        }
    }

    public async Task<Result<IDictionary<Guid, double>>> GetAllOperatorWorkloadsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var onlineOperators = await _redisManager.GetOnlineOperatorsAsync(cancellationToken);
            var workloads = new Dictionary<Guid, double>();

            foreach (var operatorId in onlineOperators)
            {
                var workloadResult = await GetOperatorWorkloadAsync(operatorId, cancellationToken);
                if (workloadResult.IsSuccess)
                {
                    workloads[operatorId] = workloadResult.Value;
                }
            }

            _logger.LogInformation("Retrieved workloads for {Count} operators", workloads.Count);

            return Result.Success<IDictionary<Guid, double>>(workloads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all operator workloads");
            return Result.Failure<IDictionary<Guid, double>>("Failed to get operator workloads", "InternalServerError");
        }
    }
}