using CRM.Chat.Application.Common.Managers;
using CRM.Chat.Application.Common.Specifications.Operators;
using CRM.Chat.Domain.Entities.Operators;
using CRM.Chat.Domain.Entities.Operators.Enums;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Operators.Queries.SetOperatorStatus;

public sealed record SetOperatorStatusCommand(
    OperatorStatus Status
) : IRequest<Unit>;

public sealed class SetOperatorStatusCommandValidator : AbstractValidator<SetOperatorStatusCommand>
{
    public SetOperatorStatusCommandValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Valid operator status is required.");
    }
}

public sealed class SetOperatorStatusCommandHandler(
    IRepository<ChatOperator> operatorRepository,
    IRedisManager redisManager,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<SetOperatorStatusCommandHandler> logger) : IRequestHandler<SetOperatorStatusCommand, Unit>
{
    public async ValueTask<Result<Unit>> Handle(SetOperatorStatusCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<Unit>("User must be authenticated", "Unauthorized");
            }

            var operatorSpec = new OperatorByUserIdSpec(userContext.Id);
            var chatOperator = await operatorRepository.FirstOrDefaultAsync(operatorSpec, cancellationToken);

            if (chatOperator == null)
            {
                return Result.Failure<Unit>("Operator profile not found", "NotFound");
            }

            if (request.Status == OperatorStatus.Offline)
            {
                chatOperator.SetOffline();
                await redisManager.SetOperatorOfflineAsync(userContext.Id, cancellationToken);
            }
            else
            {
                if (!chatOperator.IsOnline)
                {
                    chatOperator.SetOnline();
                    await redisManager.SetOperatorOnlineAsync(userContext.Id, "default", cancellationToken);
                }

                chatOperator.SetStatus(request.Status);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Notify about status change
            await notificationService.NotifyOperatorStatusChangedAsync(
                userContext.Id,
                request.Status.ToString(),
                cancellationToken);

            logger.LogInformation("Operator {OperatorId} status changed to {Status}",
                userContext.Id, request.Status);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting operator {OperatorId} status to {Status}",
                userContext.Id, request.Status);
            return Result.Failure<Unit>("Failed to set operator status", "InternalServerError");
        }
    }
}