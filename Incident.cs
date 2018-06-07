
namespace Bleep
{
  public class Incident
  {
    public Incident(string id, string studentName, string room, string teacher, string userId)
    {
      Id = id;
      StudentName = studentName;
      Room = room;
      Teacher = teacher;
      UserId = userId;
    }

    public string Id { get; set; }
    public string StudentName { get; set; }
    public string Room { get; set; }
    public string Teacher { get; set; }
    public string UserId { get; set; }
    public int Index { get; set; } = 0;
    public int Attempt { get; set; } = 1;
    public bool WentToVoicemail { get; set; } = false;
  }
}