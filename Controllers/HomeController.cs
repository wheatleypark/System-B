using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bleep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Bleep.Controllers
{
  public class HomeController : Controller
  {
    public IConfiguration Configuration { get; set; }
    public Students Students { get; set; }
    public IMemoryCache Cache { get; set; }
    public List<CallSequenceItem> CallSequence { get; set; }

    public HomeController(IConfiguration configuration, IHostingEnvironment hostingEnvironment, IMemoryCache cache, IOptions<List<CallSequenceItem>> callSequenceOptions)
    {
      Students = new Students(configuration["Personnel:SheetId"], hostingEnvironment.ContentRootPath, cache);
      Configuration = configuration;
      Cache = cache;
      CallSequence = callSequenceOptions.Value;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
      if (!User.Identity.IsAuthenticated)
      {
        return View("SignIn");
      }

      var model = new BleepRequestModel(await Students.GetAsync());
      return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Route("/post")]
    public async Task<IActionResult> Post(string studentName, string room, int priority)
    {
      if (string.IsNullOrWhiteSpace(studentName) || string.IsNullOrWhiteSpace(room) || priority < 1 || priority > 2)
      {
        return StatusCode(500, "Invalid inputs.");
      }
      if (!BritishTime.IsSchoolHours())
      {
        return StatusCode(500, "This service is unavailable out of hours.");
      }

      var userId = HttpContext.User.Identity.Name;
      var incident = new Incident(Guid.NewGuid().ToString("D"), studentName.Split(',')[0].ToTitleCase(), room, userId);

      if (priority == 1)
      {
        Cache.Set(incident.Id, incident, TimeSpan.FromMinutes(20));
        var phoner = new Phoner();
        await phoner.CallAsync(incident, CallSequence[0], Configuration["Twilio:FromNumber"], HttpContext);
      }

      var mailer = new Mailer(Configuration["Email:SenderEmail"], Configuration["Email:SenderPassword"], Configuration["Email:To"], Configuration["Email:Bcc"]);
      await mailer.SendAsync(studentName, room, User.Identity.Name, priority, User.Identity.GetEmail());
      await StatusHub.UpdateClientAsync(userId, "emailSent", priority, HttpContext);

      return new EmptyResult();
    }
  }
}