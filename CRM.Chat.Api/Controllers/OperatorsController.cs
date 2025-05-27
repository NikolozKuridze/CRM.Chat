using CRM.Chat.Api.Controllers.Base;
using CRM.Chat.Application.Common.Abstractions.Mediators;
using CRM.Chat.Application.Features.Chats.Queries.GetUserChats;
using CRM.Chat.Application.Features.Operators.Queries.GetOperatorChats;
using CRM.Chat.Application.Features.Operators.Queries.SetOperatorStatus;
using CRM.Chat.Domain.Entities.Operators.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Chat.Api.Controllers;

[Authorize]
public class OperatorsController : BaseController
{
    public OperatorsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> SetStatus([FromBody] SetStatusRequest request, CancellationToken cancellationToken)
    {
        return await SendAsync(new SetOperatorStatusCommand(request.Status), cancellationToken);
    }

    [HttpGet("my-chats")]
    [ProducesResponseType(typeof(IEnumerable<ChatSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IResult> GetMyChats([FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(new GetOperatorChatsQuery(null, activeOnly), cancellationToken);
    }

    [HttpGet("{operatorId:guid}/chats")]
    [ProducesResponseType(typeof(IEnumerable<ChatSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IResult> GetOperatorChats(Guid operatorId, [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        return await SendAsync(new GetOperatorChatsQuery(operatorId, activeOnly), cancellationToken);
    }
}

public record SetStatusRequest(OperatorStatus Status);