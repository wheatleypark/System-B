using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using System.Linq;
using Twilio;
using System.Collections.Generic;

namespace Bleep
{
  public class Startup
  {
    private IHostingEnvironment Environment { get; }
    private IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration, IHostingEnvironment environment)
    {
      Configuration = configuration;
      Environment = environment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton(Configuration);
      services.Configure<List<CallSequenceItem>>(Configuration.GetSection("CallSequence"));

      services
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(o =>
        {
          o.LoginPath = "/auth/signin";
          o.LogoutPath = "/auth/signout";
          o.ExpireTimeSpan = TimeSpan.FromDays(90);
        })
        .AddGoogle(o =>
        {
          var domain = Configuration["Auth:Google:Domain"];
          o.ClientId = Configuration["Auth:Google:ClientId"];
          o.ClientSecret = Configuration["Auth:Google:ClientSecret"];
          o.Events = new OAuthEvents()
          {
            OnRedirectToAuthorizationEndpoint = context =>
            {
              context.Response.Redirect(context.RedirectUri + "&hd=" + domain);
              return Task.CompletedTask;
            },
            OnTicketReceived = context =>
            {
              var email = context.Principal.Identity.GetEmail().ToLowerInvariant();
              var lastTwoStart = email.Length - domain.Length - 3;
              if (!email.EndsWith("@" + domain) || lastTwoStart < 0 || email.Substring(lastTwoStart, 2).All(char.IsDigit))
              {
                context.Response.Redirect("/auth/unauthorised");
                context.HandleResponse();
              }
              return Task.CompletedTask;
            }
          };
        });

      services.AddMemoryCache();

      services.AddMvc(config =>
      {
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        config.Filters.Add(new AuthorizeFilter(policy));
        if (!Environment.IsDevelopment())
        {
          config.Filters.Add(new RequireHttpsAttribute());
        }
      });

      services.AddSignalR();

      TwilioClient.Init(Configuration["Twilio:AccountSid"], Configuration["Twilio:AuthToken"]);
    }

    public void Configure(IApplicationBuilder app)
    {
      if (Environment.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseRewriter(new RewriteOptions().AddRedirectToHttps());
        app.UseHsts(hsts => hsts.MaxAge(365).IncludeSubdomains().Preload());
        app.UseXContentTypeOptions();
        app.UseReferrerPolicy(opts => opts.NoReferrer());
        app.UseXXssProtection(options => options.EnabledWithBlockMode());
        app.UseXfo(options => options.Deny());
        app.UseCsp(opts => opts
          .BlockAllMixedContent()
          .FormActions(s => s.Self())
          .FrameAncestors(s => s.Self())
        );
        app.UseExceptionHandler("/error");
      }

      app.UseStaticFiles();
      app.UseAuthentication();

      app.UseMvc(routes => {
        routes.MapRoute(
            name: "default",
            template: "{controller=Home}/{action=Index}/{id?}");
      });

      app.UseSignalR(routes =>
      {
        routes.MapHub<StatusHub>("hub");
      });
    }
  }
}