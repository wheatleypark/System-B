using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Twilio.TwiML;
using Twilio.TwiML.Voice;
using Task = System.Threading.Tasks.Task;

namespace Bleep.Controllers
{
  [AllowAnonymous]
  public class TwilioController : Controller
  {
    public IConfiguration Configuration { get; set; }
    public List<CallSequenceItem> CallSequence { get; set; }
    public IMemoryCache Cache { get; set; }

    public TwilioController(IConfiguration configuration, IOptions<List<CallSequenceItem>> callSequenceOptions, IMemoryCache cache)
    {
      Configuration = configuration;
      CallSequence = callSequenceOptions.Value;
      Cache = cache;
    }

    public async Task NextStage(Incident incident)
    {
      incident.WentToVoicemail = false;
      var stage = CallSequence[incident.Index];
      if (stage.Attempts > 1 && incident.Attempt < stage.Attempts)
      {
        incident.Attempt++;
        await StatusHub.UpdateClientAsync(incident.Teacher, "phoneDelay", stage.Delay.ToString(), HttpContext);
        await Task.Delay(stage.Delay * 1000);
      }
      else
      {
        incident.Index++;
        incident.Attempt = 1;
        if (incident.Index >= CallSequence.Count)
        {
          await StatusHub.UpdateClientAsync(incident.Teacher, "phoneFail", null, HttpContext);
          return;
        }
        stage = CallSequence[incident.Index];
      }
      var phoner = new Phoner();
      await phoner.CallAsync(incident, stage, Configuration["Twilio:FromNumber"], HttpContext);
    }

    [HttpPost, Route("/twilio/status")]
    public async Task<IActionResult> StatusCallback(string incidentId, string callStatus)
    {
      if (incidentId == null || !Cache.TryGetValue(incidentId, out Incident incident))
      {
        return StatusCode(401, "Incident token not recognised.");
      }
      if (incident.Index > -1)
      {
        var stage = CallSequence[incident.Index];
        if (callStatus == "busy" || callStatus == "no-answer" || callStatus == "failed" || callStatus == "canceled" || incident.WentToVoicemail || stage.RequireKeyPress)
        {
          await NextStage(incident);
        }
        else
        {
          incident.Index = -1;
          await StatusHub.UpdateClientAsync(incident.Teacher, "phoneDone", null, HttpContext);
        }
      }
      return Content("Handled.", "text/plain");
    }

    [HttpPost, Route("/twilio/keypress")]
    public async Task<IActionResult> KeyPress(string incidentId, string callStatus, string digits)
    {
      if (incidentId == null || !Cache.TryGetValue(incidentId, out Incident incident))
      {
        return StatusCode(401, "Incident token not recognised.");
      }
      if (digits == "1")
      {
        incident.Index = -1;
        await StatusHub.UpdateClientAsync(incident.Teacher, "phoneDone", null, HttpContext);
        var response = new VoiceResponse()
          .Say($"Thank you for agreeing to support in {incident.Room}.", Say.VoiceEnum.Woman)
          .Pause(1)
          .Say("Goodbye.", Say.VoiceEnum.Woman)
          .Hangup();
        return Content(response.ToString(), "text/xml");
      }
      else
      {
        return Index(incidentId, "human");
      }
    }

    [HttpPost, Route("/twilio")]
    public IActionResult Index(string incidentId, string answeredBy)
    {
      if (incidentId == null || !Cache.TryGetValue(incidentId, out Incident incident) || incident.Index == -1)
      {
        return StatusCode(401, "Incident token not recognised.");
      }

      if (answeredBy == "machine_start")
      {
        incident.WentToVoicemail = true;
        return Content(new VoiceResponse().Hangup().Redirect().ToString(), "text/xml");
      }

      var stage = CallSequence[incident.Index];
      var message = stage.Message
        .Replace("{student}", incident.StudentName, StringComparison.InvariantCultureIgnoreCase)
        .Replace("{room}", incident.Room, StringComparison.InvariantCultureIgnoreCase)
        .Replace("{teacher}", incident.Teacher, StringComparison.InvariantCultureIgnoreCase);

      VoiceResponse response;
      if (!stage.RequireKeyPress)
      {
        response = new VoiceResponse()
        .Say("Hello. " + message, Say.VoiceEnum.Woman)
        .Pause(1)
        .Say(message, Say.VoiceEnum.Woman)
        .Pause(1)
        .Say(message, Say.VoiceEnum.Woman)
        .Hangup();
      }
      else
      {
        var webAddress = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}/twilio/keypress?incidentId={incidentId}";
        var gather = new Gather(numDigits: 1, action: new Uri(webAddress))
        .Say("Hello. " + message, Say.VoiceEnum.Woman)
        .Pause(1)
        .Say(message, Say.VoiceEnum.Woman)
        .Pause(1)
        .Say(message, Say.VoiceEnum.Woman)
        .Pause(5);
        response = new VoiceResponse().Append(gather).Hangup();
      }
      return Content(response.ToString(), "text/xml");
    }
  }
}