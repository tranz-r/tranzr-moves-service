using ErrorOr;
using Mediator;
using Microsoft.Extensions.Logging;

using TranzrMoves.Domain.Constants;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Application.Features.Contact.SendContactForm;

public record SendContactFormCommand(
    string FirstName,
    string LastName,
    string Email,
    string? Company,
    string Subject,
    string Message,
    string TurnstileToken) : IRequest<ErrorOr<SendContactFormResponse>>;

public record SendContactFormResponse(
    bool Success,
    string Message);

public class SendContactFormCommandHandler(
    IEmailService emailService,
    ITemplateService templateService,
    ITurnstileService turnstileService,
    ILogger<SendContactFormCommandHandler> logger)
    : IRequestHandler<SendContactFormCommand, ErrorOr<SendContactFormResponse>>
{
    public async ValueTask<ErrorOr<SendContactFormResponse>> Handle(
        SendContactFormCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Processing contact form submission from {Email}", command.Email);

            // Validate Turnstile token
            var turnstileValidation = await turnstileService.ValidateTokenAsync(command.TurnstileToken, cancellationToken: cancellationToken);
            if (turnstileValidation.IsError)
            {
                logger.LogWarning("Turnstile validation failed for contact form submission from {Email}", command.Email);
                return Error.Validation("ContactForm.TurnstileValidation", "Security verification failed. Please try again.");
            }

            // Prepare template data
            var templateData = new
            {
                firstName = command.FirstName,
                lastName = command.LastName,
                email = command.Email,
                company = command.Company ?? "Not provided",
                subject = command.Subject,
                message = command.Message,
                submittedAt = DateTimeOffset.Now,
                currentYear = DateTime.Now.Year
            };

            // Generate HTML and text email content
            var htmlContent = templateService.GenerateEmail("contact-form.html", templateData);
            var textContent = templateService.GenerateEmail("contact-form-text.txt", templateData);

            // Send email to TRANZR Group
            await emailService.SendBookingConfirmationEmailAsync(
                // fromEMail: command.Email,
                fromEMail: FromEmails.Group,
                subject: $"Contact Form Submission: {command.Subject}",
                toEmail: "info@tranzrgroup.com",
                htmlEmail: htmlContent,
                textEmail: textContent);

            logger.LogInformation("Contact form email sent successfully for {Email}", command.Email);

            return new SendContactFormResponse(
                Success: true,
                Message: "Thank you for your message. We'll get back to you soon.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send contact form email for {Email}", command.Email);
            return Error.Failure("ContactForm.SendFailed", "Failed to send your message. Please try again later.");
        }
    }
}
