using CRM.Chat.Domain.Common.Models;

namespace CRM.Chat.Application.Common.Services;

public interface ILoadBalancingService
{
    Task<Result<Guid>> FindBestAvailableOperatorAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<Guid>>> GetAvailableOperatorsAsync(CancellationToken cancellationToken = default);
    Task<Result<double>> GetOperatorWorkloadAsync(Guid operatorId, CancellationToken cancellationToken = default);
    Task<Result<IDictionary<Guid, double>>> GetAllOperatorWorkloadsAsync(CancellationToken cancellationToken = default);
}