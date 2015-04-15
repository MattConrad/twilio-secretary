# twilio-secretary
Twilio secretary acting as a front layer for voice calls, and a text-to-email gateway (both directions) for text messages.

This is an ASP.NET MVC project providing a few methods that are used to manage Twilio (and also work with Zapier for text-to-email). It was mostly written for playing around with Twilio and seeing what it could do, but the text-to-email gateway might be handy to anyone who doesn't already have that on their phone.

It's built to run on Azure. Of course you'll need a different Azure site that belongs to you to run it, the pubxml won't work for you.

You'll need your own HiddenSettings.config also. You can copy HiddenSettings.sample.config and alter it.

Feel free to contact me if you have questions: mattconrad@gmail.com.

