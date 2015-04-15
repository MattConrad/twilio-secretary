using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Twilio;
using Twilio.TwiML;
using Twilio.TwiML.Mvc;

namespace TwilioABC.Controllers
{
    public class HomeController : TwilioController
    {
        TwilioRestClient _client = new TwilioRestClient("AC38987a7dcc56d590d5346680add6b3fc", "f6a56fd380499afcd89b17a4ba795681");

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult InitiateCall()
        {
            CallOptions options = new CallOptions();

            options.From = "13165182869";
            options.To = "13165294451";

            options.Url = Url.Action("SimpleCall", null, null, Request.Url.Scheme);

            var call = _client.InitiateOutboundCall(options);

            string content = "you shoulda gotten a call";

            if (call.RestException != null) content = (call.RestException.Message + call.RestException.MoreInfo);

            return Content(content);
        }

        public ActionResult SimpleCall()
        {
            var response = new TwilioResponse();

            response.Say("This is a test message. To where do we send it?");

            return TwiML(response);
        }

        public ActionResult IncomingCall()
        {
            var response = new TwilioResponse();

            response.Say("This message is rejected, if the From parameter is not null.");

            if (Request["From"] != null)
            {
                response.Reject("you stink!");
            }

            return TwiML(response);
        }

    }
}