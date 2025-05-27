using CRM.Chat.Application.Common.Services;

namespace CRM.Chat.Api.Services;

public class ChatReassignmentBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ChatReassignmentBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Check every 2 minutes

    public ChatReassignmentBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ChatReassignmentBackgroundService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Chat Reassignment Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInactiveChatsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing inactive chats");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait a bit before retrying
            }
        }

        _logger.LogInformation("Chat Reassignment Background Service stopped");
    }

    private async Task ProcessInactiveChatsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var assignmentService = scope.ServiceProvider.GetRequiredService<IChatAssignmentService>();

        var inactiveChatsResult = await assignmentService.GetInactiveChatsForReassignmentAsync(cancellationToken);

        if (inactiveChatsResult.IsFailure)
        {
            _logger.LogWarning("Failed to get inactive chats: {Error}", inactiveChatsResult.ErrorMessage);
            return;
        }

        var inactiveChats = inactiveChatsResult.Value;

        if (!inactiveChats.Any())
        {
            _logger.LogDebug("No inactive chats found for reassignment");
            return;
        }

        _logger.LogInformation("Found {Count} inactive chats for potential reassignment", inactiveChats.Count());

        foreach (var chatId in inactiveChats)
        {
            try
            {
                var reassignResult = await assignmentService.ReassignChatAsync(chatId, cancellationToken);

                if (reassignResult.IsSuccess)
                {
                    _logger.LogInformation("Successfully reassigned inactive chat {ChatId} to operator {OperatorId}",
                        chatId, reassignResult.Value);
                }
                else
                {
                    _logger.LogWarning("Failed to reassign inactive chat {ChatId}: {Error}",
                        chatId, reassignResult.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning chat {ChatId}", chatId);
            }
        }
    }
}