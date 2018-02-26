
namespace Bleep
{
  public class CallSequenceItem
  {
    public string Name { get; set; }
    public string Number { get; set; }
    public string Message { get; set; }
    public int Attempts { get; set; }
    public int Delay { get; set; }
    public bool RequireKeyPress { get; set; }
  }
}