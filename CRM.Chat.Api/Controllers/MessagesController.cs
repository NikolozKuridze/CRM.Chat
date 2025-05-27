using CRM.Chat.Api.Controllers.Base;
using CRM.Chat.Application.Common.Abstractions.Mediators;
using CRM.Chat.Application.Features.Messages.Commands.MarkMessageAsRead;
using CRM.Chat.Application.Features.Messages.Commands.SendMessage;
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