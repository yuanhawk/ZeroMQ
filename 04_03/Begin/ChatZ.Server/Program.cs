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
      var publish = string.Format($"{Default.HostAddress}:{Default.PublishPort}");
      var idle    = (long) Default.IdleMilliseconds;

      using (var context = new Context())
      { 
        // configure control socket
        var ctlSock = context.Router();
        ctlSock.Bind(control);
        WriteLine($"server receiving at {control}");

        // configure publish socket
        var pubSock = context.Pub();
        pubSock.Bind(publish);
        WriteLine($"server broadcast at {publish}");

        // initialize server state
        var clients = new Dictionary<string, DateTimeOffset>();
        while (true)
        {
          // purge expired clients
          var cutoff = DateTimeOffset.UtcNow;
          var expired = from p in clients where p.Value <= cutoff select p.Key;

          foreach (var client in expired.ToList())
          {
            clients.Remove(client);
            pubSock.SendAll(NewsMessage(GroupSender, $"Goodbye, {client}."));
            WriteLine($"INFO: {client} expired");
          }

          // poll for next client request
          if (ctlSock.TryGetInput(idle, out byte[][] message))
          {
            switch (ClientMessage.Decode(message))
            { 
              case ClientMessage.Here msg:
                if (!clients.ContainsKey(msg.Sender))
                {
                  // brand-new client!
                  pubSock.SendAll(NewsMessage(GroupSender, $"Welcome, {msg.Sender}."));
                  WriteLine($"INFO: {msg.Sender} joined");
                }

                // add/update client expiration
                clients[msg.Sender] = cutoff.AddSeconds(lease);
                
                // acknowledge heartbeat
                var reply = ListMessage(message[0], clients.Keys);
                ctlSock.SendAll(reply);
                break;

              default:
                // received an invalid message
                WriteLine("WARN: Unknown message");
                break;
            }
          }
        }
      }
    }
  }
}
