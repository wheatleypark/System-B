using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Bleep
{
  public class Mailer
  {
    private MailAddress FromAddress { get; set; }
    private MailAddress ToAddress { get; set; }
    private string BccAddresses { get; set; }
    private NetworkCredential Credential { get; set; }

    public Mailer(string senderEmail, string senderPassword, string toEmail, string bccEmails)
    {
      Credential = new NetworkCredential(senderEmail, senderPassword);
      FromAddress = new MailAddress(senderEmail, "WPS Behaviour");
      ToAddress = new MailAddress(toEmail, "Bleep Alerts");
      BccAddresses = bccEmails;
    }

    public async Task SendAsync(string studentName, string room, string teacherName, int priority, string teacherEmail)
    {
      using (var client = new SmtpClient("smtp.gmail.com") { Port = 587, Credentials = Credential, EnableSsl = true })
      {
        var message = new MailMessage
        {
          From = FromAddress,
          Subject = priority == 1 ? $"Pick up from {room}: {studentName}" : $"Sent to parking: {studentName}",
          IsBodyHtml = false,
          Body = priority == 1 ? $"{teacherName} requests urgent support." : $"Removed by {teacherName} from {room}."
        };
        message.To.Add(ToAddress);
        message.CC.Add(new MailAddress(teacherEmail, teacherName));
        message.Bcc.Add(BccAddresses);

        await client.SendMailAsync(message);
      }
    }
  }
}
