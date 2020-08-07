using ChatZ.Common;
using fszmq;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using static ChatZ.Common.Protocol;
using static System.Diagnostics.Trace;

namespace ChatZ.Client
{
  public static class Proxy // Serves a go btw for relay server & WPF user interface
  {
    private static readonly Subject<ServerMessage.List> userStream_ = new Subject<ServerMessage.List>(); // Create observable subject, connected user info, push info to user interface
    public static Task Start(Config config, CancellationToken token)  // Create background task that does the communication to the server
      => Task.Run(() => 
      {
        // initialize context
        using (var context = new Context())
        {
          // configure control socket
          var ctlSock = context.Dealer(); // Dealer socket hook into asynchronous msg exchange
          ctlSock.SetOption(ZMQ.IDENTITY, config.Handle); // Set different identity property to be nickname of user using checkline
          ctlSock.SetOption(ZMQ.RCVTIMEO, config.Timeout); // set option received timeout, num of secs that zmq shld wait while blocking to receive msg fr server
          ctlSock.Connect(config.Control); // Connect to Endpoint of server
          
          // poll in a loop (unless told to shutdown)
          while (!token.IsCancellationRequested)
          {
            try
            {
              ctlSock.SendAll(HereMessage()); // send 2 frames of heremessage to server

              var reply = ServerMessage.Decode(ctlSock.RecvAll()); // Wait to receive reply fr server, pass to decode & validate
              if (reply is ServerMessage.List msg) { userStream_.OnNext(msg); } // If msg is validated successfully, update stream of info pushed to the ui
            }
            catch (TimeoutException) // Looping diagnostic msg
            {
              WriteLine("Timeout when waiting for reply");
            }
          }
        }
      }
      , token);
    
    /// <summary>
    /// Various settings used to turn ZMQ client behavior
    /// </summary>
    public sealed class Config
    { 
      public string Control { get; }
      public int    Timeout { get; }
      public string Handle  { get; }

      public Config(string control, int timeout, string handle)
      { 
        Control = control;
        Timeout = timeout;
        Handle  = handle;
      }
    }
  }
}
