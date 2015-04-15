using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Web.Configuration;
using Twilio;
using Twilio.TwiML;
using Twilio.TwiML.Mvc;

namespace TwilioABC.Controllers
{
    public class SMSController : TwilioController
    {
        string _twilioAccountSid = WebConfigurationManager.AppSettings["TwilioAccountSid"];
        string _twilioAuthToken = WebConfigurationManager.AppSettings["TwilioAuthToken"];
        string _twilioSMSNumber = WebConfigurationManager.AppSettings["TwilioSMSNumber"];
        string _webhookedEmail = WebConfigurationManager.AppSettings["WebhookedEmail"];
        string _smsReceiverEmail = WebConfigurationManager.AppSettings["SendSMSToEmail"];
        string _adminEmail = WebConfigurationManager.AppSettings["AdminEmail"];

        public ActionResult Secretary()
        {
            //for now, the secretary accepts SMS messages, echoes them to your real phone, and sends out emails to your preferred address.
            var response = new Twilio.TwiML.TwilioResponse();

            string from = Request["From"];
            string[] blacklist = WebConfigurationManager.AppSettings["SMSBlacklist"].Split(new char[] { ',' }).Select(s => s.Trim()).ToArray();
            if (blacklist.Contains(from))
            {
                return TwiML(response);
            }

            string sid = Request["MessageSid"];
            string body = Request["Body"];
            string emailSubject = "You've got TEXT MESSAGE! from " + from;
            string emailBody = string.Format(@"{0}

---
(You can send a text back by replying to this email. Type only at the top of your reply email. Don't change any quoted text.)

[[Sid: {1}]]", body, sid);

            //target can reply back to this email, replies will have to be recieved by an email addr that is able to 
            // convert the email into a webhook back to ~/SMS/Email2SMS (webhooked email). I use Zapier (zapier.com) for this.
            Helpers.Email.SendEmailSG(_webhookedEmail, new string[] { _smsReceiverEmail }, emailSubject, emailBody, false);

            string echoSmsTo = WebConfigurationManager.AppSettings["EchoSMSMessagesTo"];
            if (!string.IsNullOrEmpty(echoSmsTo))
            {
                response.Message(body, new { to = echoSmsTo, from = _twilioSMSNumber });
            }

            return TwiML(response);
        }

        //this is a Zapier action, not a Twilio action. Our txt2email reply went to a Zapier email addr, which then hooks it back to here. Password is configured in the Zapier zap setup. 
        [ValidateInput(false)]
        public ActionResult Email2SMS(string from, string subject, string body, string password)
        {
            if (password != WebConfigurationManager.AppSettings["Email2SMSPassword"])
            {
                Helpers.Email.SendEmailSG(_adminEmail, new string[] { _adminEmail }, "Bad Email2SMS password", "someone sent " + password, false);
                return new System.Web.Mvc.HttpStatusCodeResult(200);
            }

            var twilio = new TwilioRestClient(_twilioAccountSid, _twilioAuthToken);

            string smsTo = "";
            var reSid = new Regex(@"\[\[Sid: (.*?)\]\]");
            var matches = reSid.Matches(body);
            if (matches.Count > 0)
            {
                var origSms = twilio.GetMessage(matches[0].Groups[1].Value);

                if (origSms != null && origSms.From != null) smsTo = origSms.From;
            }

            if (smsTo == "")
            {
                Helpers.Email.SendEmailSG(_adminEmail, new string[] { _adminEmail }, "Bad Email2SMS content", "Failed to retrieve original SMS: " + body, false);
                return new System.Web.Mvc.HttpStatusCodeResult(200);
            }

            string smsMessage = GetEmail2SMSReplyBody("sms.1ihur@zapiermail.com", body);
            if (smsMessage == "" || smsMessage.Length > 640)
            {
                Helpers.Email.SendEmailSG(_adminEmail, new string[] { from }, "ERROR: Could not convert your email to text message:", "smsMessage invalid, was empty or too long.\n\n" + smsMessage, false);
                return new System.Web.Mvc.HttpStatusCodeResult(200);
            }

            string[] smsMessages = ChunkString(smsMessage, 160).ToArray();
            foreach (string smsMsg in smsMessages)
            {
                var twilioReply = twilio.SendMessage("+13167126912", smsTo, smsMsg);
            }

            return new System.Web.Mvc.HttpStatusCodeResult(200);
        }

        //public ActionResult TestSMS(string txt)
        //{
        //    var twilio = new TwilioRestClient(_twilioAccountSid, _twilioAuthToken);

        //    var twilioReply = twilio.SendMessage(_twilioSMSNumber, _twilioSMSNumber, txt);

        //    return Content("no exceptions sending test text, anyway");
        //}

        private string GetEmail2SMSReplyBody(string secretaryEmailFrom, string body)
        {
            //got this regex list from http://stackoverflow.com/questions/278788/parse-email-content-from-quoted-reply , slightly modified to (usually) grab the full line of the match.
            // wrong user content in body could cause a bad match, but it shouldn't be very likely.
            List<Regex> seekers = new List<Regex>();
            seekers.Add(new Regex(".*From:\\s*" + Regex.Escape(secretaryEmailFrom) + ".*", RegexOptions.IgnoreCase));
            seekers.Add(new Regex(".*<" + Regex.Escape(secretaryEmailFrom) + ">.*", RegexOptions.IgnoreCase));
            seekers.Add(new Regex(".*" + Regex.Escape(secretaryEmailFrom) + "\\s+wrote:.*", RegexOptions.IgnoreCase));
            seekers.Add(new Regex("\\n.*On.*(\\r\\n)?wrote:\\r\\n", RegexOptions.IgnoreCase | RegexOptions.Multiline));
            seekers.Add(new Regex(".*-+original\\s+message-+\\s*$", RegexOptions.IgnoreCase));
            seekers.Add(new Regex(".*from:\\s*$", RegexOptions.IgnoreCase));

            //find what our new/quoted delimiting string is, split on it, then take whatever was above the split. there probably is cleaner way than the regex list, but this will do.
            //this SHOULD work for both plaintext and HTML replies. (html replies still contain plaintext at the top of the body, followed by HTML)
            // there are probably email clients this won't work for, update list above if needed

            string delimiter = "";
            foreach(Regex seeker in seekers)
            {
                var matches = seeker.Matches(body);

                if (matches.Count > 0)
                {
                    delimiter = matches[0].Groups[0].Value;
                    break;
                }
            }

            if (delimiter == "")
            {
                return "";
            }
            else
            {
                return body.Split(new string[] { delimiter }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
            }
        }

        private IEnumerable<string> ChunkString(string str, int chunkSize) {
            for (int index = 0; index < str.Length; index += chunkSize) {
                yield return str.Substring(index, Math.Min(chunkSize, str.Length - index));
            }
        }
    }
}