using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bleep.Controllers
{
  public class AuthController : Controller
  {
    [AllowAnonymous]
    [Route("/auth/signin")]
    public IActionResult SignIn()
    {
      var query = Request.Query["ReturnUrl"];
      var authProperties = new AuthenticationProperties
      {
        RedirectUri = query.Count == 0 ? "/" : WebUtility.UrlDecode(query.ToString()),
        AllowRefresh = true,
        IsPersistent = true
      };
      return new ChallengeResult(GoogleDefaults.AuthenticationScheme, authProperties);
    }

    [AllowAnonymous]
    [Route("/auth/unauthorised")]
    public IActionResult Unauthorised()
    {
      return View();
    }

    [Route("/auth/signout")]
    public async Task<IActionResult> SignOut()
    {
      await HttpContext.SignOutAsync();
      return Redirect("~/");
    }
  }
}