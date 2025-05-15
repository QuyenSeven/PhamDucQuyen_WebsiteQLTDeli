using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Configuration;

namespace QLyNhaHangTDeli.Services
{
    public static class EmailService
    {
        public static void SendEmail(string toEmail, string subject, string body)
        {
            var email = ConfigurationManager.AppSettings["EmailUsername"];
            var password = ConfigurationManager.AppSettings["EmailPassword"];
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(email,password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            smtpClient.Send(mailMessage);
        }
    }

}