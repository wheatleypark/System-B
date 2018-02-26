using System;

namespace Bleep
{
  public static class BritishTime
  {
    private static readonly TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

    public static DateTime Get()
    {
      var time = DateTime.UtcNow;
      return tzi.IsDaylightSavingTime(time) ? time.AddHours(1) : time;
    }

    public static bool IsSchoolHours()
    {
      var date = Get();
      var day = date.DayOfWeek;
      var hours = date.Hour;
      var mins = date.Minute;
      return (day > DayOfWeek.Sunday && day < DayOfWeek.Saturday && (hours > 8 || (hours == 8 && mins >= 15)) && (hours < 15 || (hours == 15 && mins <= 10)));
    }
  }
}