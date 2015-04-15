using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Configuration;
using Twilio;
using Twilio.TwiML;
using Twilio.TwiML.Mvc;

namespace TwilioABC.Controllers
{
    public class VoiceController : TwilioController
    {
        public ActionResult Secretary()
        {
            var response = new Twilio.TwiML.TwilioResponse();

            string digits = Request["Digits"] != null ? Request["Digits"] : "";
            string from = Request["From"] ?? "invalid";

            string[] blacklist = WebConfigurationManager.AppSettings["VoiceBlacklist"].Split(new char[] { ',' }).Select(s => s.Trim()).ToArray();
            if (blacklist.Contains(from))
            {
                response.Say("You have, unfortunately, fallen into Cindy's blacklist. Probably this should not have happened. Contact her by email or another phone number to be removed from the blacklist.");
                response.Hangup();

                return TwiML(response);
            }

            #region Record/Transcribe (commented)
            //I wanted to try this out, and it works, but I think most people just turn off their phone when they don't want calls, so not updating/maintaining it for now.
            //  kept in comment as an example of using Twilio record/transcribe
            //DateTime cstNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time"));
            //if (cstNow.Hour < 8 || cstNow.Hour > 16)
            //{
            //    response.Say("It is late, and Cindy is probably asleep. Leave a message and he will return your call tomorrow.");
            //    response.Record(new
            //    {
            //        transcribe = "true",
            //        transcribeCallback = Url.Action("EmailTranscription", "Voice", new { emailTo = "matt@wichitaprogrammer.com" }, Request.Url.Scheme),
            //        maxLength = "30"
            //    });
            //    response.Hangup();

            //    return TwiML(response);
            //}
            #endregion

            //check and see if this call passes for any reason, and if so, forward to the destination number
            string[] whitelistAreaCodes= WebConfigurationManager.AppSettings["VoiceWhitelistAreaCodes"].Split(new char[] { ',' }).Select(s => s.Trim()).ToArray();
            string[] whitelist= WebConfigurationManager.AppSettings["VoiceWhitelist"].Split(new char[] { ',' }).Select(s => s.Trim()).ToArray();
            string voicePIN = WebConfigurationManager.AppSettings["VoicePublicPasscode"];
            if (whitelist.Contains(from) || whitelistAreaCodes.Any(ac => from.StartsWith(ac)) || digits == voicePIN)
            {
                string voiceForwardCallsTo = WebConfigurationManager.AppSettings["VoiceForwardCallsTo"];
                response.Dial(voiceForwardCallsTo);

                return TwiML(response);
            }

            //either prompt for a passcode or reject a failed passcode 
            if (digits == "")
            {
                response.BeginGather(new { numdigits = "4" })
                    .Say("I don't recognize the number you're calling from. Enter the passcode followed by the pound sign and I will ring you through.");
                response.EndGather();

                return TwiML(response);
            }
            else
            {
                response.Say("Sorry, I don't recognize that passcode either. You can get the passcode from Cindy or Matt, and try again.");
                response.Say("Or you can contact Cindy via email instead.");
                response.Say("Goodbye.");
                response.Hangup();

                return TwiML(response);
            }
        }

        //this is only for transcription, which is currently inactive. kept as a comment as an example of how-to.
        //public ActionResult EmailTranscription()
        //{
        //    string emailTo = Request["emailTo"];
        //    string status = Request["TranscriptionStatus"];
        //    string xcription = Request["TranscriptionText"] ?? "";
        //    string messageUrl = Request["RecordingUrl"];

        //    string[] whitelistRecipients = new string[] { "mattconrad@gmail.com", "matt@wichitaprogrammer.com" };
        //    if (!whitelistRecipients.Contains(emailTo))
        //    {
        //        throw new ArgumentException("Invalid recipient email address.");
        //    }

        //    string subject = "Received Twilio voice mail:";
        //    string body = string.Format("Received recorded message. \n\nStatus: {0} \n\nTranscription: {1} \n\nMessage Url: {2}", status, xcription, messageUrl);
        //    Helpers.Email.SendEmailSG("matt@wichitaprogrammer.com", new string[] { "matt@wichitaprogrammer.com" }, subject, body, false);

        //    return new System.Web.Mvc.HttpStatusCodeResult(200);
        //}

    }
}