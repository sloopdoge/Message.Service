using System.Net;
using System.Net.Mail;
using Message.Domain.Enums;
using Message.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Message.Infrastructure.Services;

public class EmailSenderService : ISenderService
{
    public MessageType Type => MessageType.Email;

    private readonly ILogger<EmailSenderService> _logger;
    private readonly SmtpClient _smtpClient;

    private string SenderAddress { get; set; }

    public EmailSenderService(ILogger<EmailSenderService> logger,
        IConfiguration configuration)
    {
        _logger = logger;

        var smtpSettings = configuration.GetSection("SmtpSettings");
        SenderAddress = smtpSettings["From"];

        _smtpClient = new SmtpClient
        {
            Host = smtpSettings["Host"],
            Port = int.Parse(smtpSettings["Port"]),
            EnableSsl = bool.Parse(smtpSettings["EnableSsl"]),
            Credentials = new NetworkCredential(
                smtpSettings["Username"],
                smtpSettings["Password"])
        };
    }

    public async Task<bool> Send(Domain.Entities.Message message)
    {
        try
        {
            using var mailMessage = new MailMessage
            {
                From = new MailAddress(SenderAddress),
                Subject = message.Subject ?? "(no subject)",
                Body = message.Body,
                IsBodyHtml = false,
            };

            message.Recipient.ForEach(recipient => mailMessage.To.Add(recipient));

            await _smtpClient.SendMailAsync(mailMessage);

            _logger.LogInformation("Email sent to {Recipient}", message.Recipient);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient}", message.Recipient);
            throw;
        }
    }
}