using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Bleep
{
  public class Phoner
  {
    public Phoner() { }

    public async Task CallAsync(Incident incident, CallSequenceItem stage, string fromNumber, HttpContext ctx)
    {
      var webAddress = $"{ctx.Request.Scheme}://{ctx.Request.Host.Value}";
      var query = "?incidentId=" + incident.Id;

      await StatusHub.UpdateClientAsync(incident.UserId, "phoneStart", stage.Name + (incident.Attempt > 1 ? $" (attempt {incident.Attempt} of {stage.Attempts})" : string.Empty), ctx);

      await CallResource.CreateAsync(
        to: new PhoneNumber(stage.Number),
        from: new PhoneNumber(fromNumber),
        url: new Uri(webAddress + "/twilio" + query),
        statusCallback: new Uri(webAddress + "/twilio/status" + query),
        statusCallbackEvent: new List<string> { "completed" },
        machineDetection: stage.RequireKeyPress ? "Enable" : null
      );
    }
  }
}