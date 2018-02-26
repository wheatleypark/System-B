using System.Linq;
using System.Security.Claims;
using System.Security.Principal;

namespace Bleep
{
  public static class IdentityHelper
  {
    private static string GetClaim(this IIdentity identity, string claimType)
    {
      var claimsIdentity = identity as ClaimsIdentity;
      var claim = claimsIdentity?.Claims.FirstOrDefault(o => o.Type == claimType);
      return claim?.Value;
    }

    public static string GetEmail(this IIdentity identity)
    {
      return identity.GetClaim(ClaimTypes.Email).ToLowerInvariant();
    }

    public static string GetUserId(this IIdentity identity)
    {
      return identity.GetClaim(ClaimTypes.NameIdentifier);
    }
  }
}
