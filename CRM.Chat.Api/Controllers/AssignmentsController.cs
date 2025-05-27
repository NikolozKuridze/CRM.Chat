using CRM.Chat.Api.Controllers.Base;
using CRM.Chat.Application.Common.Abstractions.Mediators;
using CRM.Chat.Application.Features.Messages.Commands.MarkMessageAsRead;
using CRM.Chat.Application.Features.Messages.Commands.SendMessage;
using CRM.Chat.Domain.Entities.Messages.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRM.Chat.Api.Controllers;
