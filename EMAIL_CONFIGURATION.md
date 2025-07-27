# Email Configuration for Tranzr Moves API

This document explains how to configure email functionality using AWS Simple Email Service (SES) with MailKit.

## Required Environment Variables

Add the following environment variables to your configuration:

```bash
# AWS SES SMTP Configuration
AWS_SES_SMTP_HOST=email-smtp.eu-west-1.amazonaws.com
AWS_SES_SMTP_PORT=587
AWS_SES_SMTP_USERNAME=your-ses-smtp-username
AWS_SES_SMTP_PASSWORD=your-ses-smtp-password

# Email Sender Configuration
FROM_EMAIL=noreply@tranzrmoves.com
FROM_NAME=Tranzr Moves
```

## AWS SES Setup

1. **Create an AWS SES account** (if you don't have one)
2. **Verify your domain** in AWS SES
3. **Create SMTP credentials**:
   - Go to AWS SES Console
   - Navigate to "SMTP settings"
   - Create SMTP credentials
   - Use the provided username and password

## Email Functionality

The email service will automatically send order confirmation emails when:
- A payment is successfully processed via Stripe
- The webhook receives a `payment_intent.succeeded` event

## Email Template Features

The order confirmation email includes:
- Rich HTML styling with responsive design
- Order details (ID, date, amount, service)
- Professional branding
- Contact information
- Mobile-friendly layout

## Testing

To test the email functionality:
1. Set up the environment variables
2. Process a test payment through Stripe
3. Check that the webhook receives the success event
4. Verify the email is sent to the customer

## Troubleshooting

- Check logs for email service errors
- Verify AWS SES credentials are correct
- Ensure the FROM_EMAIL domain is verified in AWS SES
- Check that the SMTP port (587) is not blocked by your network 