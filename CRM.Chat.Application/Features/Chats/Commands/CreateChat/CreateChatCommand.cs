using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Chats.Commands.CreateChat;

public sealed record CreateChatCommand(
    string Title,
    ChatType Type,
    string? Description = null,
    int Priority = 1,
    Guid? TargetOperatorId = null
) : IRequest<Guid>;

public sealed class CreateChatCommandValidator : AbstractValidator<CreateChatCommand>
{
    public CreateChatCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .WithMessage("Title is required and must not exceed 200 characters.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Valid chat type is required.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(1, 10)
            .WithMessage("Priority must be between 1 and 10.");
    }
}

public sealed class CreateChatCommandHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IChatAssignmentService assignmentService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<CreateChatCommandHandler> logger) : IRequestHandler<CreateChatCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(CreateChatCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<Guid>("User must be authenticated to create chat", "Unauthorized");
            }

            var chat = new Domain.Entities.Chats.Chat(
                request.Title,
                request.Type,
                userContext.Id,
                request.Description,
                request.Priority);

            await chatRepository.AddAsync(chat, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // For customer support chats, automatically assign to an operator
            if (request.Type == ChatType.CustomerSupport)
            {
                if (request.TargetOperatorId.HasValue)
                {
                    // Assign to specific operator if specified
                    await assignmentService.TransferChatAsync(chat.Id, request.TargetOperatorId.Value,
                        "Initial assignment", cancellationToken);
                }
                else
                {
                    // Auto-assign to best available operator
                    await assignmentService.AssignChatToOperatorAsync(chat.Id, cancellationToken);
                }
            }

            logger.LogInformation("Chat {ChatId} created by user {UserId} with type {ChatType}",
                chat.Id, userContext.Id, request.Type);

            return Result.Success(chat.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating chat for user {UserId}", userContext.Id);
            return Result.Failure<Guid>("Failed to create chat", "InternalServerError");
        }
    }
}