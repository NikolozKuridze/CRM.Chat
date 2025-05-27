using CRM.Chat.Application.Common.Specifications.Chats;
using CRM.Chat.Domain.Entities.Messages;
using CRM.Chat.Domain.Entities.Messages.Enums;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Application.Features.Messages.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ChatId,
    string Content,
    MessageType Type = MessageType.Text
) : IRequest<Guid>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ChatId)
            .NotEmpty()
            .WithMessage("Chat ID is required.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(4000)
            .WithMessage("Message content is required and must not exceed 4000 characters.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Valid message type is required.");
    }
}

public sealed class SendMessageCommandHandler(
    IRepository<Domain.Entities.Chats.Chat> chatRepository,
    IRepository<ChatMessage> messageRepository,
    INotificationService notificationService,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    ILogger<SendMessageCommandHandler> logger) : IRequestHandler<SendMessageCommand, Guid>
{
    public async ValueTask<Result<Guid>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!userContext.IsAuthenticated)
            {
                return Result.Failure<Guid>("User must be authenticated to send message", "Unauthorized");
            }

            var chatSpec = new ChatByIdWithMessagesSpec(request.ChatId);
            var chat = await chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<Guid>("Chat not found", "NotFound");
            }

            // Check if user can send message to this chat
            var canSendMessage = chat.InitiatorId == userContext.Id ||
                                 chat.AssignedOperatorId == userContext.Id ||
                                 chat.Participants.Any(p => p.UserId == userContext.Id && p.IsActive);

            if (!canSendMessage)
            {
                return Result.Failure<Guid>("Insufficient permissions to send message", "Forbidden");
            }

            // Check if chat is closed
            if (chat.Status == Domain.Entities.Chats.Enums.ChatStatus.Closed)
            {
                return Result.Failure<Guid>("Cannot send message to closed chat", "BadRequest");
            }

            var message = new ChatMessage(request.ChatId, userContext.Id, request.Content, request.Type);

            await messageRepository.AddAsync(message, cancellationToken);

            // Update chat activity
            chat.UpdateActivity();

            await unitOfWork.SaveChangesAsync(cancellationToken);

            // Send real-time notification
            await notificationService.NotifyNewMessageAsync(
                request.ChatId,
                message.Id,
                userContext.Id,
                cancellationToken);

            logger.LogInformation("Message {MessageId} sent to chat {ChatId} by user {UserId}",
                message.Id, request.ChatId, userContext.Id);

            return Result.Success(message.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending message to chat {ChatId} by user {UserId}",
                request.ChatId, userContext.Id);
            return Result.Failure<Guid>("Failed to send message", "InternalServerError");
        }
    }
}