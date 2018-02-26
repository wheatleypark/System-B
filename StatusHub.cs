using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace Bleep
{
  public class StatusHub : Hub
  {
    public async Task UpdateClient(string userId, string command, object data)
    {
      await Clients.User(userId).InvokeAsync(nameof(UpdateClient), command, data);
    }

    public static async Task UpdateClientAsync(string clientId, string command, object data, HttpContext ctx)
    {
      var connection = new HubConnectionBuilder().WithUrl($"{ctx.Request.Scheme}://{ctx.Request.Host.Value}/hub").Build();
      await connection.StartAsync();
      await connection.InvokeAsync(nameof(UpdateClient), clientId, command, data);
    }
  }
}
