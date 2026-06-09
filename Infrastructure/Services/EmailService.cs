using Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MimeKit.Text;

namespace Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse(_config["EmailSettings:From"]));
        email.To.Add(MailboxAddress.Parse(to));
        email.Subject = subject;
        email.Body = new TextPart(TextFormat.Html) { Text = body };

        using var smtp = new SmtpClient();
        
        // Conexión para Gmail (Puerto 587 con STARTTLS)
        await smtp.ConnectAsync(
            _config["EmailSettings:Host"], 
            int.Parse(_config["EmailSettings:Port"]), 
            SecureSocketOptions.StartTls);

        await smtp.AuthenticateAsync(
            _config["EmailSettings:UserName"], 
            _config["EmailSettings:Password"]);

        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
