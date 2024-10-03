using System.Net.Mail;
using System.Text;
using OneWithReplication.Contracts;
using OneWithReplication.Settings;

namespace OneWithReplication.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(EmailSettings settings)
    {
        if (ValidationService.ValidateEmailSettings(settings, out string errorMessage))
        {
            _settings = settings;
        }
        else
        {
            throw new Exception($"Неправильные настройки электронной почты! Следующие параметры не указаны: {errorMessage}");
        }
    }

    public void SendMessage(string body)
    {
        try
        {
            SmtpClient smtp = new(_settings.SmtpServer, _settings.Port);
            using MailMessage message = new();
            Encoding encoding = Encoding.UTF8;
            message.IsBodyHtml = false;
            message.SubjectEncoding = encoding;
            message.BodyEncoding = encoding;
            message.From = new MailAddress(_settings.From, _settings.From, encoding);
            message.Bcc.Add(_settings.To);
            message.Subject = "Отчёт о сохранении баз в облако";
            message.Body = body;
            smtp.EnableSsl = true;
            smtp.Credentials = new System.Net.NetworkCredential(_settings.UserName, _settings.Password);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.Send(message);
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка '{ex.Message}' при отправке письма с сервера {_settings.SmtpServer}, порт {_settings.Port}");
        }
    }
}