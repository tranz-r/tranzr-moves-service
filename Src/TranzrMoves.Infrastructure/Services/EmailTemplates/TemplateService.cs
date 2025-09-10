using HandlebarsDotNet;
using Microsoft.Extensions.Logging;

namespace TranzrMoves.Infrastructure.Services.EmailTemplates;

public interface ITemplateService
{
    string GenerateEmail(string templateName, object data);
}

public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly Dictionary<string, HandlebarsTemplate<object, object>> _compiledTemplates;
    private readonly string _templatesPath;

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
        _compiledTemplates = new Dictionary<string, HandlebarsTemplate<object, object>>();
        _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "EmailTemplates", "Templates");
        
        RegisterHelpers();
        CompileTemplates();
    }

    private void RegisterHelpers()
    {
        // Register custom helpers for formatting
        Handlebars.RegisterHelper("formatCurrency", (context, arguments) =>
        {
            if (arguments.Length > 0 && decimal.TryParse(arguments[0]?.ToString(), out var amount))
            {
                return amount.ToString("N2");
            }
            return "0.00";
        });

        Handlebars.RegisterHelper("formatDate", (context, arguments) =>
        {
            if (arguments.Length > 0 && DateTime.TryParse(arguments[0]?.ToString(), out var date))
            {
                return date.ToString("dddd, MMMM dd, yyyy");
            }
            return DateTime.Now.ToString("dddd, MMMM dd, yyyy");
        });

        Handlebars.RegisterHelper("formatTime", (context, arguments) =>
        {
            if (arguments.Length > 0 && DateTime.TryParse(arguments[0]?.ToString(), out var date))
            {
                return date.ToString("HH:mm") + " GMT";
            }
            return DateTime.Now.ToString("HH:mm") + " GMT";
        });
    }

    private void CompileTemplates()
    {
        try
        {
            if (!Directory.Exists(_templatesPath))
            {
                _logger.LogWarning("Templates directory not found: {TemplatesPath}", _templatesPath);
                return;
            }

            var templateFiles = Directory.GetFiles(_templatesPath, "*.hbs", SearchOption.TopDirectoryOnly);
            
            foreach (var templateFile in templateFiles)
            {
                var templateName = Path.GetFileNameWithoutExtension(templateFile);
                var templateContent = File.ReadAllText(templateFile);
                
                try
                {
                    var compiledTemplate = Handlebars.Compile(templateContent);
                    _compiledTemplates[templateName] = compiledTemplate;
                    _logger.LogInformation("Successfully compiled template: {TemplateName}", templateName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to compile template: {TemplateName}", templateName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load templates from directory: {TemplatesPath}", _templatesPath);
        }
    }

    public string GenerateEmail(string templateName, object data)
    {
        try
        {
            if (!_compiledTemplates.TryGetValue(templateName, out var template))
            {
                _logger.LogError("Template not found: {TemplateName}", templateName);
                throw new ArgumentException($"Template '{templateName}' not found. Available templates: {string.Join(", ", _compiledTemplates.Keys)}");
            }

            var result = template(data);
            _logger.LogDebug("Successfully generated email using template: {TemplateName}", templateName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate email using template: {TemplateName}", templateName);
            throw;
        }
    }

    public void ReloadTemplates()
    {
        _logger.LogInformation("Reloading templates...");
        _compiledTemplates.Clear();
        CompileTemplates();
        _logger.LogInformation("Templates reloaded successfully");
    }
}
