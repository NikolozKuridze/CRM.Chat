using CRM.Chat.Api.Controllers.Base;
using CRM.Chat.Application.Common.Abstractions.Mediators;
using CRM.Chat.Application.Features.Messages.Commands.DeleteMessage;
using CRM.Chat.Application.Features.Messages.Commands.EditMessage;
using CRM.Chat.Application.Features.Messages.Commands.MarkMessageAsRead;
using CRM.Chat.Application.Features.Messages.Commands.SendMessage;
using CRM.Chat.Application.Features.Messages.Commands.SendMessageWithFile;
using CRM.Chat.Application.Features.Messages.Queries.GetMessagesByChatId;
using CRM.Chat.Domain.Entities.Messages.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Chat.Api.Controllers;

[Authorize]
public class MessagesController : BaseController
{
    public MessagesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet("chat/{chatId:guid}")]
    [ProducesResponseType(typeof(PagedResult<MessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> GetChatMessages(
        Guid chatId,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(new GetMessagesByChatIdQuery(chatId, pageIndex, pageSize), cancellationToken);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(
            request.ChatId,
            request.Content,
            request.Type);

        return await SendAsync(command, cancellationToken);
    }

    [HttpPost("with-file")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> SendMessageWithFile([FromBody] SendMessageWithFileRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendMessageWithFileCommand(
            request.ChatId,
            request.Content,
            request.FileId);

        return await SendAsync(command, cancellationToken);
    }

    [HttpPut("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request,
        CancellationToken cancellationToken)
    {
        return await SendAsync(new EditMessageCommand(messageId, request.NewContent), cancellationToken);
    }

    [HttpDelete("{messageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> DeleteMessage(Guid messageId, CancellationToken cancellationToken)
    {
        return await SendAsync(new DeleteMessageCommand(messageId), cancellationToken);
    }

    [HttpPost("{messageId:guid}/mark-read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> MarkMessageAsRead(Guid messageId, CancellationToken cancellationToken)
    {
        return await SendAsync(new MarkMessageAsReadCommand(messageId), cancellationToken);
    }
}

public record SendMessageRequest(
    Guid ChatId,
    string Content,
    MessageType Type = MessageType.Text);

public record SendMessageWithFileRequest(
    Guid ChatId,
    string Content,
    Guid FileId);

public record EditMessageRequest(
    string NewContent);