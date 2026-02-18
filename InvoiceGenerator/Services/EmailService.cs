using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace InvoiceGenerator.Services
{
    public class EmailService
    {
        /// <summary>
        /// Sends an invoice via email with PDF attachment
        /// </summary>
        public async System.Threading.Tasks.Task SendInvoiceEmailAsync(
            string smtpServer,
            int smtpPort,
            string fromEmail,
            string fromPassword,
            string toEmail,
            string subject,
            string body,
            string pdfAttachmentPath,
            bool useTls = true)
        {
            try
            {
                if (!File.Exists(pdfAttachmentPath))
                    throw new FileNotFoundException($"PDF file not found: {pdfAttachmentPath}");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Invoice Generator", fromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    TextBody = body,
                    HtmlBody = body
                };

                // Attach PDF
                await bodyBuilder.Attachments.AddAsync(pdfAttachmentPath);

                message.Body = bodyBuilder.ToMessageBody();

                // Send via SMTP
                using (var client = new SmtpClient())
                {
                    var secureSocketOption = useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                    await client.ConnectAsync(smtpServer, smtpPort, secureSocketOption);
                    await client.AuthenticateAsync(fromEmail, fromPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tests SMTP connection and credentials
        /// </summary>
        public async System.Threading.Tasks.Task TestSmtpConnectionAsync(
            string smtpServer,
            int smtpPort,
            string fromEmail,
            string fromPassword,
            bool useTls = true)
        {
            try
            {
                using (var client = new SmtpClient())
                {
                    var secureSocketOption = useTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                    await client.ConnectAsync(smtpServer, smtpPort, secureSocketOption);
                    await client.AuthenticateAsync(fromEmail, fromPassword);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"SMTP connection test failed: {ex.Message}", ex);
            }
        }
    }
}
