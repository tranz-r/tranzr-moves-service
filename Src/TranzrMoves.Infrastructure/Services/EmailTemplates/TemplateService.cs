using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;

using TranzrMoves.Application.Common.Time;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services.EmailTemplates;

public class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly ITimeService _timeService;
    private readonly Dictionary<string, HandlebarsTemplate<object, object>> _compiledTemplates;
    private readonly string _templatesPath;

    public TemplateService(ILogger<TemplateService> logger, ITimeService timeService)
    {
        _logger = logger;
        _timeService = timeService;
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
            if (arguments.Length > 0 && TryParseTemplateInstant(arguments[0]?.ToString(), out var instant))
            {
                var z = instant.InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault());
                return LocalDatePattern.CreateWithCurrentCulture("dddd, MMMM dd, yyyy").Format(z.Date);
            }

            var localNow = _timeService.Now()
                .InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            return LocalDatePattern.CreateWithCurrentCulture("dddd, MMMM dd, yyyy").Format(localNow.Date);
        });

        Handlebars.RegisterHelper("formatTime", (context, arguments) =>
        {
            if (arguments.Length > 0 && TryParseTemplateInstant(arguments[0]?.ToString(), out var instant))
            {
                return LocalTimePattern.CreateWithInvariantCulture("HH:mm").Format(instant.InUtc().TimeOfDay) + " GMT";
            }

            var utcNow = _timeService.NowInUtc();
            return LocalTimePattern.CreateWithInvariantCulture("HH:mm").Format(utcNow.TimeOfDay) + " GMT";
        });
    }

    private static bool TryParseTemplateInstant(string? text, out Instant instant)
    {
        instant = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var s = text.Trim();
        var extended = InstantPattern.ExtendedIso.Parse(s);
        if (extended.Success)
        {
            instant = extended.Value;
            return true;
        }

        var localDate = LocalDatePattern.Iso.Parse(s);
        if (localDate.Success)
        {
            instant = localDate.Value.AtStartOfDayInZone(DateTimeZone.Utc).ToInstant();
            return true;
        }

        return false;
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
