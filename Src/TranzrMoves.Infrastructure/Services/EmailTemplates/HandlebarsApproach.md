# Handlebars.Net Email Templates

This approach uses Handlebars.Net for even cleaner template management with external files.

## Installation

Add the Handlebars.Net NuGet package:

```bash
dotnet add package Handlebars.Net
```

## Template Files Structure

Create a `Templates` folder in your project:

```
TranzrMoves.Infrastructure/
├── Services/
│   ├── EmailTemplates/
│   │   ├── Templates/
│   │   │   ├── deposit-confirmation.html.hbs
│   │   │   ├── deposit-confirmation.txt.hbs
│   │   │   ├── balance-confirmation.html.hbs
│   │   │   ├── balance-confirmation.txt.hbs
│   │   │   ├── full-payment-confirmation.html.hbs
│   │   │   ├── full-payment-confirmation.txt.hbs
│   │   │   ├── setup-confirmation.html.hbs
│   │   │   └── setup-confirmation.txt.hbs
│   │   └── TemplateService.cs
```

## Example Template Files

### deposit-confirmation.html.hbs
```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Deposit Confirmation - Tranzr Moves</title>
    <style>
        /* CSS styles here */
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Deposit Confirmation</h1>
            <p>Your deposit has been received</p>
        </div>
        
        <div class="content">
            <div class="greeting">
                <p>Dear <strong>{{customerName}}</strong>,</p>
            </div>
            
            <p>Thank you for your deposit payment with Tranzr Moves. We're pleased to confirm that your deposit has been successfully processed and your booking is now secured.</p>
            
            <div class="payment-details">
                <h2>Payment Summary</h2>
                <div class="payment-row">
                    <span class="payment-label">Quote Reference:</span>
                    <span class="payment-value">#{{quoteReference}}</span>
                </div>
                <div class="payment-row">
                    <span class="payment-label">Payment Date:</span>
                    <span class="payment-value">{{paymentDate}}</span>
                </div>
                <div class="payment-row">
                    <span class="payment-label">Payment Time:</span>
                    <span class="payment-value">{{paymentTime}}</span>
                </div>
                <div class="payment-row">
                    <span class="payment-label">Deposit Amount:</span>
                    <span class="payment-value">£{{depositAmount}}</span>
                </div>
                <div class="payment-row">
                    <span class="payment-label">Total Amount:</span>
                    <span class="payment-value">£{{totalAmount}}</span>
                </div>
                <div class="payment-row">
                    <span class="payment-label">Remaining Balance:</span>
                    <span class="payment-value">£{{remainingAmount}}</span>
                </div>
            </div>
            
            <div class="deposit-message">
                <h3>✓ Deposit Received</h3>
                <p>Your deposit has been processed successfully. Your booking is now confirmed and secured.</p>
            </div>
            
            <div class="next-steps">
                <h4>What Happens Next?</h4>
                <ul>
                    <li>Our team will contact you within 24 hours to confirm your moving date</li>
                    <li>We'll provide detailed service information and final arrangements</li>
                    <li>The remaining balance of £{{remainingAmount}} will be due on the day of your move</li>
                    <li>You can pay the balance using cash, card, or bank transfer</li>
                </ul>
            </div>
            
            <div class="contact-info">
                <h4>Customer Support</h4>
                <p><strong>Email:</strong> support@tranzrmoves.com</p>
                <p><strong>Phone:</strong> +44 (0) 20 1234 5678</p>
                <p><strong>Hours:</strong> Monday - Friday, 8:00 AM - 6:00 PM GMT</p>
            </div>
        </div>
        
        <div class="footer">
            <p><strong>Tranzr Moves Ltd</strong></p>
            <p>Professional moving services across the United Kingdom</p>
            <div class="company-info">
                <p>&copy; {{currentYear}} Tranzr Moves. All rights reserved.</p>
                <p>Registered in England & Wales | Company No: 12345678</p>
            </div>
        </div>
    </div>
</body>
</html>
```

### deposit-confirmation.txt.hbs
```
DEPOSIT CONFIRMATION - TRANZR MOVES

Dear {{customerName}},

Thank you for your deposit payment with Tranzr Moves. We're pleased to confirm that your deposit has been successfully processed and your booking is now secured.

PAYMENT SUMMARY
===============
Quote Reference: #{{quoteReference}}
Payment Date: {{paymentDate}}
Payment Time: {{paymentTime}}
Deposit Amount: £{{depositAmount}}
Total Amount: £{{totalAmount}}
Remaining Balance: £{{remainingAmount}}

✓ DEPOSIT RECEIVED
Your deposit has been processed successfully. Your booking is now confirmed and secured.

WHAT HAPPENS NEXT?
=================
• Our team will contact you within 24 hours to confirm your moving date
• We'll provide detailed service information and final arrangements
• The remaining balance of £{{remainingAmount}} will be due on the day of your move
• You can pay the balance using cash, card, or bank transfer

CUSTOMER SUPPORT
================
Email: support@tranzrmoves.com
Phone: +44 (0) 20 1234 5678
Hours: Monday - Friday, 8:00 AM - 6:00 PM GMT

---
Tranzr Moves Ltd
Professional moving services across the United Kingdom
© {{currentYear}} Tranzr Moves. All rights reserved.
Registered in England & Wales | Company No: 12345678
```

## Template Service Implementation

```csharp
using HandlebarsDotNet;
using Microsoft.Extensions.Logging;

namespace TranzrMoves.Infrastructure.Services.EmailTemplates;

public class TemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly Dictionary<string, HandlebarsTemplate<object, object>> _templates = new();
    private readonly string _templatePath;

    public TemplateService(ILogger<TemplateService> logger)
    {
        _logger = logger;
        _templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates");
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        try
        {
            var templateFiles = Directory.GetFiles(_templatePath, "*.hbs", SearchOption.AllDirectories);
            
            foreach (var file in templateFiles)
            {
                var templateName = Path.GetFileNameWithoutExtension(file);
                var templateContent = File.ReadAllText(file);
                var template = Handlebars.Compile(templateContent);
                _templates[templateName] = template;
                
                _logger.LogInformation("Loaded template: {TemplateName}", templateName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load email templates");
        }
    }

    public string GenerateEmail(string templateName, object data)
    {
        if (!_templates.ContainsKey(templateName))
        {
            _logger.LogError("Template not found: {TemplateName}", templateName);
            throw new ArgumentException($"Template '{templateName}' not found");
        }

        try
        {
            return _templates[templateName](data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate email from template: {TemplateName}", templateName);
            throw;
        }
    }
}
```

## Usage in CheckoutController

```csharp
// In CheckoutController constructor
private readonly TemplateService _templateService;

public CheckoutController(/* other dependencies */, TemplateService templateService)
{
    _templateService = templateService;
    // ... other assignments
}

// In HandlePaymentIntentSucceeded
if (paymentType == nameof(PaymentType.Deposit))
{
    var depositAmount = paymentIntent.Amount / 100.0m;
    var totalCost = quoteDto.Pricing?.TotalCost ?? depositAmount;
    var remainingAmount = totalCost - depositAmount;
    
    var templateData = new
    {
        customerName = customerName,
        depositAmount = depositAmount.ToString("N2"),
        totalAmount = totalCost.ToString("N2"),
        remainingAmount = remainingAmount.ToString("N2"),
        quoteReference = quoteReference,
        paymentDate = orderDate.ToString("dddd, MMMM dd, yyyy"),
        paymentTime = orderDate.ToString("HH:mm") + " GMT",
        currentYear = DateTime.UtcNow.Year
    };
    
    var subject = $"Deposit Confirmation - #{quoteReference}";
    var htmlEmail = _templateService.GenerateEmail("deposit-confirmation.html", templateData);
    var textEmail = _templateService.GenerateEmail("deposit-confirmation.txt", templateData);
    
    await awsEmailService.SendBookingConfirmationEmailAsync(subject, customer.Email, htmlEmail, textEmail);
}
```

## Benefits of Handlebars.Net Approach

1. **External Files**: Templates are completely separate from code
2. **Clean Syntax**: Simple `{{variableName}}` syntax
3. **Conditionals**: Support for `{{#if condition}}...{{/if}}`
4. **Loops**: Support for `{{#each items}}...{{/each}}`
5. **Helpers**: Custom functions for complex logic
6. **Version Control**: Templates can be versioned separately
7. **Designer Friendly**: Non-developers can edit templates
8. **Hot Reload**: Templates can be reloaded without restarting the application

## Comparison

| Aspect | Option 1 (Static Classes) | Option 2 (Handlebars.Net) |
|--------|---------------------------|---------------------------|
| **File Organization** | ✅ Separate classes | ✅ External .hbs files |
| **Variable Injection** | ✅ String interpolation | ✅ Template variables |
| **Conditionals/Loops** | ❌ Limited | ✅ Full support |
| **Designer Friendly** | ❌ Code knowledge needed | ✅ HTML/CSS knowledge only |
| **Hot Reload** | ❌ Requires rebuild | ✅ Can reload templates |
| **Complex Logic** | ✅ Full C# power | ⚠️ Limited to helpers |
| **Performance** | ✅ Compile-time | ✅ Runtime compilation |
| **Maintenance** | ⚠️ Code changes needed | ✅ Template changes only |

Both approaches are excellent improvements over the current implementation. **Option 1** is simpler and maintains full C# power, while **Option 2** provides maximum flexibility and separation of concerns.
