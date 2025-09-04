using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranzrMoves.Domain.Interfaces;
using TranzrMoves.Infrastructure.Services.EmailTemplates;

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

    private static string GenerateOrderConfirmationHtml(string customerName, long amount, string orderId, DateTime orderDate)
    {
        var amountInPounds = amount / 100.0m;
        var currentYear = DateTime.UtcNow.Year;
        
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <meta name=""description"" content=""Order confirmation for your Tranzr Moves booking"">
    <title>Order Confirmation - Tranzr Moves</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            line-height: 1.6;
            color: #2c3e50;
            margin: 0;
            padding: 0;
            background-color: #f8f9fa;
        }}
        .container {{
            max-width: 600px;
            margin: 20px auto;
            background-color: #ffffff;
            border-radius: 12px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{
            background: linear-gradient(135deg, #3498db 0%, #2980b9 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0 0 10px 0;
            font-size: 32px;
            font-weight: 600;
            letter-spacing: -0.5px;
        }}
        .header p {{
            margin: 0;
            font-size: 16px;
            opacity: 0.9;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .greeting {{
            font-size: 18px;
            margin-bottom: 25px;
            color: #2c3e50;
        }}
        .order-details {{
            background-color: #f8f9fa;
            border-radius: 10px;
            padding: 30px;
            margin: 25px 0;
            border: 1px solid #e9ecef;
        }}
        .order-details h2 {{
            margin: 0 0 20px 0;
            color: #2c3e50;
            font-size: 20px;
            font-weight: 600;
        }}
        .order-row {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin: 15px 0;
            padding: 12px 0;
            border-bottom: 1px solid #e9ecef;
        }}
        .order-row:last-child {{
            border-bottom: none;
            font-weight: 600;
            font-size: 18px;
            color: #27ae60;
            margin-top: 20px;
            padding-top: 20px;
            border-top: 2px solid #27ae60;
        }}
        .order-label {{
            color: #6c757d;
            font-weight: 500;
        }}
        .order-value {{
            font-weight: 600;
            color: #2c3e50;
        }}
        .success-message {{
            text-align: center;
            margin: 30px 0;
            padding: 25px;
            background: linear-gradient(135deg, #d4edda 0%, #c3e6cb 100%);
            border-radius: 10px;
            border: 1px solid #c3e6cb;
        }}
        .success-message h3 {{
            margin: 0 0 15px 0;
            color: #155724;
            font-size: 22px;
        }}
        .success-message p {{
            margin: 0;
            color: #155724;
            font-size: 16px;
        }}
        .contact-info {{
            margin-top: 30px;
            padding: 25px;
            background-color: #f8f9fa;
            border-radius: 10px;
            text-align: center;
            border: 1px solid #e9ecef;
        }}
        .contact-info h4 {{
            margin: 0 0 15px 0;
            color: #2c3e50;
            font-size: 18px;
        }}
        .contact-info p {{
            margin: 5px 0;
            color: #6c757d;
        }}
        .footer {{
            background-color: #2c3e50;
            color: white;
            text-align: center;
            padding: 25px;
            font-size: 14px;
        }}
        .footer p {{
            margin: 5px 0;
        }}
        .company-info {{
            margin-top: 15px;
            padding-top: 15px;
            border-top: 1px solid #34495e;
        }}
        @media only screen and (max-width: 600px) {{
            .container {{
                margin: 10px;
                border-radius: 8px;
            }}
            .header, .content {{
                padding: 20px;
            }}
            .order-row {{
                flex-direction: column;
                align-items: flex-start;
                gap: 5px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Order Confirmation</h1>
            <p>Thank you for choosing Tranzr Moves</p>
        </div>
        
        <div class=""content"">
            <div class=""greeting"">
                <p>Dear <strong>{customerName}</strong>,</p>
            </div>
            
            <p>Thank you for your recent order with Tranzr Moves. We're pleased to confirm that your booking has been successfully processed and your payment has been received.</p>
            
            <div class=""order-details"">
                <h2>Order Summary</h2>
                <div class=""order-row"">
                    <span class=""order-label"">Order Reference:</span>
                    <span class=""order-value"">#{orderId}</span>
                </div>
                <div class=""order-row"">
                    <span class=""order-label"">Order Date:</span>
                    <span class=""order-value"">{orderDate:dddd, MMMM dd, yyyy}</span>
                </div>
                <div class=""order-row"">
                    <span class=""order-label"">Order Time:</span>
                    <span class=""order-value"">{orderDate:HH:mm} GMT</span>
                </div>
                <div class=""order-row"">
                    <span class=""order-label"">Service Type:</span>
                    <span class=""order-value"">Professional Moving Service</span>
                </div>
                <div class=""order-row"">
                    <span class=""order-label"">Total Amount:</span>
                    <span class=""order-value"">£{amountInPounds:N2}</span>
                </div>
            </div>
            
            <div class=""success-message"">
                <h3>✓ Payment Confirmed</h3>
                <p>Your payment has been processed successfully. Our team will contact you within 24 hours to confirm your moving date and provide detailed service information.</p>
            </div>
            
            <div class=""contact-info"">
                <h4>Customer Support</h4>
                <p><strong>Email:</strong> support@tranzrmoves.com</p>
                <p><strong>Phone:</strong> +44 (0) 20 1234 5678</p>
                <p><strong>Hours:</strong> Monday - Friday, 8:00 AM - 6:00 PM GMT</p>
            </div>
        </div>
        
        <div class=""footer"">
            <p><strong>Tranzr Moves Ltd</strong></p>
            <p>Professional moving services across the United Kingdom</p>
            <div class=""company-info"">
                <p>&copy; {currentYear} Tranzr Moves. All rights reserved.</p>
                <p>Registered in England & Wales | Company No: 12345678</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }
} 