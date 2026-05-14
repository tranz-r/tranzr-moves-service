namespace TranzrMoves.Domain.Interfaces;

public interface ITemplateService
{
    string GenerateEmail(string templateName, object data);

    /// <summary>
    /// Re-reads and recompiles all templates from disk. Used after template file changes; thread-safe with <see cref="GenerateEmail"/>.
    /// </summary>
    void ReloadTemplates();
}
