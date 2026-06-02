using HandlebarsDotNet;
using Microsoft.Extensions.Logging;
using NodaTime;
using NodaTime.Text;
using TranzrMoves.Notifications.Infrastructure.Interfaces;

namespace TranzrMoves.Notifications.Infrastructure.EmailTemplates;

public sealed class TemplateService : ITemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly IClock _clock;
    private readonly IHandlebars _handlebars = Handlebars.Create();
    private readonly Dictionary<string, HandlebarsTemplate<object, object>> _compiledTemplates = new();
    private readonly string _templatesPath;
    private readonly object _templatesLock = new();

    public TemplateService(ILogger<TemplateService> logger, IClock clock)
    {
        _logger = logger;
        _clock = clock;
        _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "EmailTemplates", "Templates");
        RegisterHelpers();
        RegisterPartials();
        CompileTemplates();
        _logger.LogInformation("Email templates loaded from {TemplatesPath}", _templatesPath);
    }

    private void RegisterPartials()
    {
        var partialsDir = Path.Combine(_templatesPath, "partials");
        if (!Directory.Exists(partialsDir))
        {
            return;
        }

        foreach (var partialFile in Directory.GetFiles(partialsDir, "*.hbs", SearchOption.TopDirectoryOnly))
        {
            var partialName = Path.GetFileNameWithoutExtension(partialFile);
            var partialContent = File.ReadAllText(partialFile);
            _handlebars.RegisterTemplate(partialName, partialContent);
        }
    }

    private void RegisterHelpers()
    {
        _handlebars.RegisterHelper("formatCurrency", (context, arguments) =>
        {
            if (arguments.Length > 0 && decimal.TryParse(arguments[0]?.ToString(), out var amount))
            {
                return amount.ToString("N2");
            }

            return "0.00";
        });

        _handlebars.RegisterHelper("formatDate", (context, arguments) =>
        {
            if (arguments.Length > 0 && TryParseTemplateInstant(arguments[0]?.ToString(), out var instant))
            {
                var z = instant.InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault());
                return LocalDatePattern.CreateWithCurrentCulture("dddd, MMMM dd, yyyy").Format(z.Date);
            }

            var localNow = _clock.GetCurrentInstant().InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            return LocalDatePattern.CreateWithCurrentCulture("dddd, MMMM dd, yyyy").Format(localNow.Date);
        });

        _handlebars.RegisterHelper("formatTime", (context, arguments) =>
        {
            if (arguments.Length > 0 && TryParseTemplateInstant(arguments[0]?.ToString(), out var instant))
            {
                return LocalTimePattern.CreateWithInvariantCulture("HH:mm").Format(instant.InUtc().TimeOfDay) + " GMT";
            }

            var utcNow = _clock.GetCurrentInstant();
            return LocalTimePattern.CreateWithInvariantCulture("HH:mm").Format(utcNow.InUtc().TimeOfDay) + " GMT";
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
                _compiledTemplates[templateName] = _handlebars.Compile(templateContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile template: {TemplateName}", templateName);
            }
        }
    }

    public string GenerateEmail(string templateName, object data)
    {
        lock (_templatesLock)
        {
            if (!_compiledTemplates.TryGetValue(templateName, out var template))
            {
                throw new ArgumentException(
                    $"Template '{templateName}' not found. Available: {string.Join(", ", _compiledTemplates.Keys)}");
            }

            return template(data);
        }
    }

    public void ReloadTemplates()
    {
        lock (_templatesLock)
        {
            _compiledTemplates.Clear();
            RegisterPartials();
            CompileTemplates();
        }
    }
}
