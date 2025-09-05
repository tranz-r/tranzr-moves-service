using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;

namespace TranzrMoves.Infrastructure.Services;

public class AwsEmailService : IAwsEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AwsEmailService> _logger;
    IAmazonSimpleEmailServiceV2 _ses;
    private readonly string _fromEmail;

    public AwsEmailService(
        IConfiguration configuration, 
        ILogger<AwsEmailService> logger, IAmazonSimpleEmailServiceV2 ses)
    {
        _configuration = configuration;
        _logger = logger;
        _ses = ses;
        _fromEmail = _configuration["FROM_EMAIL"] ?? "noreply@tranzrmoves.com";
    }

    public async Task SendBookingConfirmationEmailAsync(string subject, string toEmail, string htmlEmail, string textEmail)
    {
        var emailRequest = new SendEmailRequest
        {
            FromEmailAddress = _fromEmail,
            Destination = new Destination
            {
                ToAddresses = [toEmail],
            },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject },
                    Body = new Body
                    {
                        Html = new Content { Data = htmlEmail },
                        Text = new Content { Data = textEmail }
                    }
                }
            }
        };
        
        try
        {
            _logger.LogInformation("Sending email using Amazon SES...");
            var response = await _ses.SendEmailAsync(emailRequest);
            _logger.LogInformation("The email was sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError("The email was not sent.");
            _logger.LogError("Error message: " + ex.Message);
            _logger.LogError(ex, "Failed to send order confirmation email to {Email}", toEmail);

        }
    }
} 