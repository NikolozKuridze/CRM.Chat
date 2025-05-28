using CRM.Chat.Application.Common.Services;

namespace CRM.Chat.Api.Services;

public class ChatReassignmentBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<ChatReassignmentBackgroundService> logger,
    IConfiguration configuration)
    : BackgroundService
{
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(
        configuration.GetValue("ChatSettings:ReassignmentCheckIntervalMinutes", 2));

    private readonly TimeSpan _inactivityThreshold = TimeSpan.FromMinutes(
        configuration.GetValue("ChatSettings:InactivityThresholdMinutes", 5));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Chat Reassignment Background Service started. Check interval: {CheckInterval}, Inactivity threshold: {InactivityThreshold}",
            _checkInterval, _inactivityThreshold);

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInactiveChatsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                logger.LogInformation("Chat reassignment service cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in chat reassignment service");
            }

            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Chat Reassignment Background Service stopped");
    }

    private async Task ProcessInactiveChatsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();

        try
        {
            var assignmentService = scope.ServiceProvider.GetRequiredService<IChatAssignmentService>();

            logger.LogDebug("Checking for inactive chats with threshold: {Threshold}", _inactivityThreshold);

            var inactiveChatsResult = await assignmentService.GetInactiveChatsForReassignmentAsync(cancellationToken);

            if (inactiveChatsResult.IsFailure)
            {
                logger.LogWarning("Failed to get inactive chats: {Error}", inactiveChatsResult.ErrorMessage);
                return;
            }

            var inactiveChats = inactiveChatsResult.Value?.ToList();

            if (inactiveChats is not null && !inactiveChats.Any())
            {
                logger.LogDebug("No inactive chats found for reassignment");
                return;
            }

            logger.LogInformation("Found {Count} inactive chats for potential reassignment", inactiveChats?.Count);

            var successCount = 0;
            var failCount = 0;

            foreach (var chatId in inactiveChats!)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    logger.LogInformation("Reassignment cancelled, processed {Success} chats successfully",
                        successCount);
                    break;
                }

                try
                {
                    var reassignResult = await assignmentService.ReassignChatAsync(chatId, cancellationToken);

                    if (reassignResult.IsSuccess)
                    {
                        successCount++;
                        logger.LogInformation(
                            "Successfully reassigned inactive chat {ChatId} to operator {OperatorId}",
                            chatId, reassignResult.Value);
                    }
                    else
                    {
                        failCount++;
                        logger.LogWarning("Failed to reassign inactive chat {ChatId}: {Error}",
                            chatId, reassignResult.ErrorMessage);
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    logger.LogError(ex, "Error reassigning chat {ChatId}", chatId);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }

            if (successCount > 0 || failCount > 0)
            {
                logger.LogInformation(
                    "Chat reassignment batch completed. Success: {SuccessCount}, Failed: {FailCount}",
                    successCount, failCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in ProcessInactiveChatsAsync");
        }
    }
}