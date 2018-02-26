using System.Collections.Generic;

namespace Bleep.Models
{
  public class BleepRequestModel
  {
    public IList<string> Students { get; set; }

    public BleepRequestModel(IList<string> students)
    {
      Students = students;
    }
  }
}