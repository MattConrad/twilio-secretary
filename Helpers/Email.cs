using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;
using System.Web.Configuration;
using SendGrid;

namespace TwilioABC.Helpers
{
    public class Email
    {
        private static string _sgUserName = WebConfigurationManager.AppSettings["SendGridUserName"];
        private static string _sgPassword =  WebConfigurationManager.AppSettings["SendGridPassword"];

        public static void SendEmailSG(string from, IEnumerable<string> to, string subject, string body, bool isHtml)
        {
            var email = new SendGridMessage();
            email.From = new MailAddress(from);
            email.AddTo(to);
            email.Subject = subject;

            if (isHtml)
            {
                email.Html = body;
            }
            else
            {
                email.Text = body;
            }

            var credentials = new NetworkCredential(_sgUserName, _sgPassword);
            var transportWeb = new Web(credentials);

            transportWeb.Deliver(email);
        }

    }
}