using CRM.Chat.Api.Controllers.Base;
using CRM.Chat.Application.Common.Abstractions.Mediators;
using CRM.Chat.Application.Features.Chats.Commands.CloseChat;
using CRM.Chat.Application.Features.Chats.Commands.CreateChat;
using CRM.Chat.Application.Features.Chats.Commands.TransferChat;
using CRM.Chat.Application.Features.Chats.Queries.GetChatById;
using CRM.Chat.Application.Features.Chats.Queries.GetUserChats;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Chat.Api.Controllers;

[Authorize]
public class ChatsController : BaseController
{
    public ChatsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> CreateChat([FromBody] CreateChatCommand command, CancellationToken cancellationToken)
    {
        return await SendAsync(command, cancellationToken);
    }

    [HttpGet("{chatId:guid}")]
    [ProducesResponseType(typeof(ChatDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> GetChatById(Guid chatId, CancellationToken cancellationToken)
    {
        return await SendAsync(new GetChatByIdQuery(chatId), cancellationToken);
    }

    [HttpGet("my-chats")]
    [ProducesResponseType(typeof(IEnumerable<ChatSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMyChats([FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(new GetUserChatsQuery(null, pageIndex, pageSize), cancellationToken);
    }

    [HttpPost("{chatId:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> CloseChat(Guid chatId, [FromBody] CloseChatRequest request,
        CancellationToken cancellationToken)
    {
        return await SendAsync(new CloseChatCommand(chatId, request.Reason), cancellationToken);
    }

    [HttpPost("{chatId:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> TransferChat(Guid chatId, [FromBody] TransferChatRequest request,
        CancellationToken cancellationToken)
    {
        return await SendAsync(new TransferChatCommand(chatId, request.NewOperatorId, request.Reason),
            cancellationToken);
    }
}

public record CloseChatRequest(string? Reason);

public record TransferChatRequest(Guid NewOperatorId, string Reason);