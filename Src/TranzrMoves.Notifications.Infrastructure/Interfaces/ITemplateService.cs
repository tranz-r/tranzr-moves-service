namespace TranzrMoves.Notifications.Infrastructure.Interfaces;

public interface ITemplateService
{
    string GenerateEmail(string templateName, object data);

    void ReloadTemplates();
}
