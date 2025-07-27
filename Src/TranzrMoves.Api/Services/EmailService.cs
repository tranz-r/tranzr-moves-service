using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TranzrMoves.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // AWS SES SMTP Configuration
        _smtpHost = _configuration["AWS_SES_SMTP_HOST"] ?? "email-smtp.eu-west-2.amazonaws.com";
        _smtpPort = int.Parse(_configuration["AWS_SES_SMTP_PORT"] ?? "587");
        _smtpUsername = _configuration["AWS_SES_SMTP_USERNAME"] ?? throw new InvalidOperationException("AWS_SES_SMTP_USERNAME is required");
        _smtpPassword = _configuration["AWS_SES_SMTP_PASSWORD"] ?? throw new InvalidOperationException("AWS_SES_SMTP_PASSWORD is required");
        _fromEmail = _configuration["FROM_EMAIL"] ?? "noreply@tranzrmoves.com";
        _fromName = _configuration["FROM_NAME"] ?? "Tranzr Moves";
    }

    public async Task SendOrderConfirmationEmailAsync(string customerEmail, string customerName, long amount, string orderId, DateTime orderDate)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_fromName, _fromEmail));
            email.To.Add(new MailboxAddress(customerName, customerEmail));
            email.Subject = $"Order Confirmation - #{orderId}";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = GenerateOrderConfirmationHtml(customerName, amount, orderId, orderDate)
            };

            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_smtpHost, _smtpPort);
            await smtp.AuthenticateAsync(_smtpUsername, _smtpPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Order confirmation email sent successfully to {Email} for order {OrderId}", customerEmail, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation email to {Email} for order {OrderId}", customerEmail, orderId);
            throw;
        }
    }

    private static string GenerateOrderConfirmationHtml(string customerName, long amount, string orderId, DateTime orderDate)
    {
        var amountInPounds = amount / 100.0m;
        
        return $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Order Confirmation</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 28px;
            font-weight: 300;
        }}
        .content {{
            padding: 40px 30px;
        }}
        .order-details {{
            background-color: #f8f9fa;
            border-radius: 8px;
            padding: 25px;
            margin: 20px 0;
            border-left: 4px solid #667eea;
        }}
        .order-row {{
            display: flex;
            justify-content: space-between;
            margin: 10px 0;
            padding: 8px 0;
            border-bottom: 1px solid #e9ecef;
        }}
        .order-row:last-child {{
            border-bottom: none;
            font-weight: bold;
            font-size: 18px;
            color: #667eea;
        }}
        .thank-you {{
            text-align: center;
            margin: 30px 0;
            padding: 20px;
            background-color: #e8f5e8;
            border-radius: 8px;
            border: 1px solid #c3e6c3;
        }}
        .footer {{
            background-color: #2c3e50;
            color: white;
            text-align: center;
            padding: 20px;
            font-size: 14px;
        }}
        .logo {{
            font-size: 24px;
            font-weight: bold;
            margin-bottom: 10px;
        }}
        .contact-info {{
            margin-top: 20px;
            padding: 20px;
            background-color: #f8f9fa;
            border-radius: 8px;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <div class=""logo"">🚚 Tranzr Moves</div>
            <h1>Order Confirmation</h1>
            <p>Thank you for choosing our moving services!</p>
        </div>
        
        <div class=""content"">
            <p>Dear <strong>{customerName}</strong>,</p>
            
            <p>We're excited to confirm your order has been successfully processed. Here are the details of your booking:</p>
            
            <div class=""order-details"">
                <div class=""order-row"">
                    <span>Order ID:</span>
                    <span><strong>#{orderId}</strong></span>
                </div>
                <div class=""order-row"">
                    <span>Order Date:</span>
                    <span>{orderDate:dddd, MMMM dd, yyyy}</span>
                </div>
                <div class=""order-row"">
                    <span>Order Time:</span>
                    <span>{orderDate:HH:mm}</span>
                </div>
                <div class=""order-row"">
                    <span>Service:</span>
                    <span>Professional Moving Service</span>
                </div>
                <div class=""order-row"">
                    <span>Total Amount:</span>
                    <span>£{amountInPounds:N2}</span>
                </div>
            </div>
            
            <div class=""thank-you"">
                <h3>🎉 Thank You!</h3>
                <p>Your payment has been processed successfully. Our team will be in touch shortly to confirm your moving date and provide you with all the necessary details.</p>
            </div>
            
            <div class=""contact-info"">
                <h4>Need Help?</h4>
                <p>If you have any questions about your order, please don't hesitate to contact us:</p>
                <p>📧 Email: support@tranzrmoves.com<br>
                📞 Phone: +44 (0) 20 1234 5678<br>
                💬 Live Chat: Available on our website</p>
            </div>
        </div>
        
        <div class=""footer"">
            <p>&copy; 2024 Tranzr Moves. All rights reserved.</p>
            <p>Professional moving services across the UK</p>
        </div>
    </div>
</body>
</html>";
    }
} 