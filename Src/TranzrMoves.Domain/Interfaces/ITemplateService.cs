namespace TranzrMoves.Domain.Interfaces;

public interface ITemplateService
{
    string GenerateEmail(string templateName, object data);
}