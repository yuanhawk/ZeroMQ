using ChatZ.Common;
using fszmq;
using System;
using System.Collections.Generic;
using System.Linq;

using static System.Console;
using static ChatZ.Common.Protocol;
using static ChatZ.Server.Properties.Settings;

namespace ChatZ.Server
{
  public static class Program
  {
    public static void Main(string[] _args)
    {
      var lease   = Default.ClientLeaseSeconds;
      var control = string.Format($"{Default.HostAddress}:{Default.ControlPort}");
      var idle    = (long) Default.IdleMilliseconds;

      // initialize context
      using (var context = new Context())
      {
        // configure control socket, create router socket, access to routing envelope, receive msg asynchronously
        var ctlSock = context.Router();
        ctlSock.Bind(control);

        // initialize server state, empty dict, maps clients handle to a DateTime that expires
        var clients = new Dictionary<string, DateTimeOffset>();
        while (true)
        {
          // purge expired clients, note current time, query existing clients to find out which are expired
          var cutoff = DateTimeOffset.UtcNow;
          var expired = from p in clients where p.Value <= cutoff select p.Key;

          foreach (var client in expired.ToList()) // Loop thru expired clients & delete from list
          {
            clients.Remove(client);
            WriteLine($"INFO: {client} expired"); // Diagnostic msg
          }

          // poll for next client request, TryGetInput returns a msg and wait up to configure Realtime period
          if (ctlSock.TryGetInput(idle, out byte[][] message))
          {
            switch (ClientMessage.Decode(message)) // Parse & Validate msg, go to Documentation for more info
            {
              case ClientMessage.Here msg: // Valid peer msg
                if (!clients.ContainsKey(msg.Sender)) // Brand new client or client tracked
                {
                  WriteLine($"INFO: {msg.Sender} joined"); // New client diagnostic msg
                }

                clients[msg.Sender] = cutoff.AddSeconds(lease); // Update clients expiry in internal state, time window date at start of loop & 5secs to it

                var reply = ListMessage(message[0], clients.Keys);
                // Send a reply back to client, first frame of msg received contains routing envelope for sender, UTF-8 json list of active clients
                ctlSock.SendAll(reply); // SendAll send multiple frames (entire msg)
                break;

              default: // Invalid msg
                WriteLine("WARN: Unknown message"); // Diagnostic Warning
                break;
            }
          }
        }
      }
    }
  }
}
