namespace Foundry.Communications.Application.Channels.Email.Commands.SendEmail;

public sealed record SendEmailCommand(
    string To,
    string? From,
    string Subject,
    string Body);
