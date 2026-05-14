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
    private readonly IHandlebars _handlebars;
    private readonly Dictionary<string, HandlebarsTemplate<object, object>> _compiledTemplates;
    private readonly string _templatesPath;
    private readonly object _templatesLock = new();

    public TemplateService(ILogger<TemplateService> logger, ITimeService timeService)
    {
        _logger = logger;
        _timeService = timeService;
        _handlebars = Handlebars.Create();
        _compiledTemplates = new Dictionary<string, HandlebarsTemplate<object, object>>();
        _templatesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "EmailTemplates", "Templates");

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
            _logger.LogInformation("Registered email template partial: {PartialName}", partialName);
        }
    }

    private void RegisterHelpers()
    {
        // Register custom helpers for formatting
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

            var localNow = _timeService.Now()
                .InZone(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            return LocalDatePattern.CreateWithCurrentCulture("dddd, MMMM dd, yyyy").Format(localNow.Date);
        });

        _handlebars.RegisterHelper("formatTime", (context, arguments) =>
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

            var templateFiles = Directory.GetFiles(_templatesPath, "*.hbs", SearchOption.TopDirectoryOnly)
                .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}partials{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    && !f.Contains($"{Path.AltDirectorySeparatorChar}partials{Path.AltDirectorySeparatorChar}", StringComparison.Ordinal))
                .ToArray();

            foreach (var templateFile in templateFiles)
            {
                var templateName = Path.GetFileNameWithoutExtension(templateFile);
                var templateContent = File.ReadAllText(templateFile);

                try
                {
                    var compiledTemplate = _handlebars.Compile(templateContent);
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
        lock (_templatesLock)
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
    }

    public void ReloadTemplates()
    {
        lock (_templatesLock)
        {
            _logger.LogInformation("Reloading templates...");
            _compiledTemplates.Clear();
            RegisterPartials();
            CompileTemplates();
            _logger.LogInformation("Templates reloaded successfully");
        }
    }
}
