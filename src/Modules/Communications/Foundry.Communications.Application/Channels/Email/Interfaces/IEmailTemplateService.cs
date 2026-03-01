namespace Foundry.Communications.Application.Channels.Email.Interfaces;

public interface IEmailTemplateService
{
    Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default);
}
