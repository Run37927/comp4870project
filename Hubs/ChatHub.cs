using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalrChat.Hubs
{
    public class ChatHub : Hub
    {
        // javascript can directly call this method
        public async Task SendMessage(string user, string message)
        {
            // the server calls a js function on the client named ReceiveMessage
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }

}