using CRM.Chat.Application.Common.Specifications.Chats;
using CRM.Chat.Application.Common.Specifications.Operators;
using Microsoft.Extensions.Logging;

namespace CRM.Chat.Infrastructure.Services;

public class ChatAssignmentService : IChatAssignmentService
{
    private readonly IRepository<Domain.Entities.Chats.Chat> _chatRepository;
    private readonly IRepository<ChatOperator> _operatorRepository;
    private readonly ILoadBalancingService _loadBalancingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChatAssignmentService> _logger;

    public ChatAssignmentService(
        IRepository<Domain.Entities.Chats.Chat> chatRepository,
        IRepository<ChatOperator> operatorRepository,
        ILoadBalancingService loadBalancingService,
        IUnitOfWork unitOfWork,
        ILogger<ChatAssignmentService> logger)
    {
        _chatRepository = chatRepository;
        _operatorRepository = operatorRepository;
        _loadBalancingService = loadBalancingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> AssignChatToOperatorAsync(Guid chatId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatSpec = new ChatByIdWithMessagesSpec(chatId);
            var chat = await _chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<Guid>("Chat not found", "NotFound");
            }

            if (chat.AssignedOperatorId.HasValue)
            {
                return Result.Failure<Guid>("Chat is already assigned", "BadRequest");
            }

            // Find the best available operator
            var operatorResult = await _loadBalancingService.FindBestAvailableOperatorAsync(cancellationToken);

            if (operatorResult.IsFailure)
            {
                return Result.Failure<Guid>("No available operators found", "NotFound");
            }

            var operatorId = operatorResult.Value;
            var operatorSpec = new OperatorByUserIdSpec(operatorId);
            var chatOperator = await _operatorRepository.FirstOrDefaultAsync(operatorSpec, cancellationToken);

            if (chatOperator == null || !chatOperator.CanAcceptNewChat())
            {
                return Result.Failure<Guid>("Selected operator cannot accept new chats", "BadRequest");
            }

            // Assign the chat
            chat.AssignOperator(operatorId);
            chatOperator.AssignChat();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Chat {ChatId} assigned to operator {OperatorId}", chatId, operatorId);

            return Result.Success(operatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning chat {ChatId} to operator", chatId);
            return Result.Failure<Guid>("Failed to assign chat", "InternalServerError");
        }
    }

    public async Task<Result<Guid>> ReassignChatAsync(Guid chatId, CancellationToken cancellationToken = default)
    {
        try
        {
            var chatSpec = new ChatByIdWithMessagesSpec(chatId);
            var chat = await _chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<Guid>("Chat not found", "NotFound");
            }

            if (!chat.CanBeReassigned())
            {
                return Result.Failure<Guid>("Chat cannot be reassigned", "BadRequest");
            }

            var previousOperatorId = chat.AssignedOperatorId;

            // Release from current operator
            if (previousOperatorId.HasValue)
            {
                var previousOperatorSpec = new OperatorByUserIdSpec(previousOperatorId.Value);
                var previousOperator =
                    await _operatorRepository.FirstOrDefaultAsync(previousOperatorSpec, cancellationToken);
                previousOperator?.UnassignChat();
            }

            // Find new operator
            var newOperatorResult = await _loadBalancingService.FindBestAvailableOperatorAsync(cancellationToken);

            if (newOperatorResult.IsFailure)
            {
                // If no operators available, leave unassigned
                chat.AssignOperator(Guid.Empty); // This would need to be modified to allow null assignment
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return Result.Failure<Guid>("No available operators for reassignment", "NotFound");
            }

            var newOperatorId = newOperatorResult.Value;
            var newOperatorSpec = new OperatorByUserIdSpec(newOperatorId);
            var newOperator = await _operatorRepository.FirstOrDefaultAsync(newOperatorSpec, cancellationToken);

            if (newOperator == null || !newOperator.CanAcceptNewChat())
            {
                return Result.Failure<Guid>("Selected operator cannot accept new chats", "BadRequest");
            }

            // Reassign the chat
            chat.AssignOperator(newOperatorId);
            newOperator.AssignChat();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Chat {ChatId} reassigned from operator {PreviousOperatorId} to {NewOperatorId}",
                chatId, previousOperatorId, newOperatorId);

            return Result.Success(newOperatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reassigning chat {ChatId}", chatId);
            return Result.Failure<Guid>("Failed to reassign chat", "InternalServerError");
        }
    }

    public async Task<Result<Guid>> TransferChatAsync(Guid chatId, Guid newOperatorId, string reason,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatSpec = new ChatByIdWithMessagesSpec(chatId);
            var chat = await _chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<Guid>("Chat not found", "NotFound");
            }

            var newOperatorSpec = new OperatorByUserIdSpec(newOperatorId);
            var newOperator = await _operatorRepository.FirstOrDefaultAsync(newOperatorSpec, cancellationToken);

            if (newOperator == null || !newOperator.CanAcceptNewChat())
            {
                return Result.Failure<Guid>("Target operator cannot accept new chats", "BadRequest");
            }

            var previousOperatorId = chat.AssignedOperatorId;

            // Update previous operator
            if (previousOperatorId.HasValue)
            {
                var previousOperatorSpec = new OperatorByUserIdSpec(previousOperatorId.Value);
                var previousOperator =
                    await _operatorRepository.FirstOrDefaultAsync(previousOperatorSpec, cancellationToken);
                previousOperator?.UnassignChat();
            }

            // Transfer the chat
            chat.TransferToOperator(newOperatorId, reason);
            newOperator.AssignChat();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Chat {ChatId} transferred to operator {NewOperatorId}. Reason: {Reason}",
                chatId, newOperatorId, reason);

            return Result.Success(newOperatorId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring chat {ChatId} to operator {NewOperatorId}", chatId, newOperatorId);
            return Result.Failure<Guid>("Failed to transfer chat", "InternalServerError");
        }
    }

    public async Task<Result<Unit>> ReleaseChatFromOperatorAsync(Guid chatId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var chatSpec = new ChatByIdWithMessagesSpec(chatId);
            var chat = await _chatRepository.FirstOrDefaultAsync(chatSpec, cancellationToken);

            if (chat == null)
            {
                return Result.Failure<Unit>("Chat not found", "NotFound");
            }

            if (!chat.AssignedOperatorId.HasValue)
            {
                return Result.Success(Unit.Value); // Already unassigned
            }

            var operatorSpec = new OperatorByUserIdSpec(chat.AssignedOperatorId.Value);
            var chatOperator = await _operatorRepository.FirstOrDefaultAsync(operatorSpec, cancellationToken);

            chatOperator?.UnassignChat();

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Chat {ChatId} released from operator {OperatorId}",
                chatId, chat.AssignedOperatorId);

            return Result.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing chat {ChatId} from operator", chatId);
            return Result.Failure<Unit>("Failed to release chat", "InternalServerError");
        }
    }

    public async Task<Result<IEnumerable<Guid>>> GetInactiveChatsForReassignmentAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var inactivityThreshold = TimeSpan.FromMinutes(5);
            var inactiveChatsSpec = new InactiveChatsSpec(inactivityThreshold);
            var inactiveChats = await _chatRepository.ListAsync(inactiveChatsSpec, cancellationToken);

            var chatIds = inactiveChats.Select(c => c.Id).ToList();

            _logger.LogInformation("Found {Count} inactive chats for potential reassignment", chatIds.Count);

            return Result.Success<IEnumerable<Guid>>(chatIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting inactive chats for reassignment");
            return Result.Failure<IEnumerable<Guid>>("Failed to get inactive chats", "InternalServerError");
        }
    }
}