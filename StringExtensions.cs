using System.Globalization;

namespace Bleep
{
  public static class StringExtensions
  {
    public static string ToTitleCase(this string s)
    {
      return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(s.ToLower());
    }
  }
}