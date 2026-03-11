using Foundry.Shared.Kernel.Identity;

namespace Foundry.Communications.Application.Channels.Sms.Commands.SendSms;

public sealed record SendSmsCommand(
    string To,
    string Body,
    string? From = null,
    UserId? UserId = null,
    string? NotificationType = null);
