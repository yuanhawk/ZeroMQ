using ChatZ.Common;
using fszmq;
using System;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using static ChatZ.Common.Protocol;
using static System.Diagnostics.Trace;

namespace ChatZ.Client
{ // extend client to receive message
  public static class Proxy
  {
    private static readonly Subject<ServerMessage.List> userStream_ = new Subject<ServerMessage.List>();
    private static readonly Subject<ServerMessage.News> newsStream_ = new Subject<ServerMessage.News>();
    // subjects recall out the pushbased mechanism by which our client send information to the WPF GUI thread
    public static Task Start(Config config, CancellationToken token) 
      => Task.Run(() => 
      {
        using (var context = new Context())
        { // Context, Server, Communication with the server
          // configure control socket
          var ctlSock = context.Dealer();
          ctlSock.SetOption(ZMQ.IDENTITY, config.Handle);
          ctlSock.SetOption(ZMQ.RCVTIMEO, config.Timeout);
          ctlSock.Connect($"{config.Control}");

          // configure subscriber socket
          var subSock = context.Sub();
          subSock.Subscribe(new[] { GroupTopic }); // Set subscription, sockets will not receive msg unless subscription is set, constructed the byte array and pass it to subscribe method of the socket
          subSock.SetOption(ZMQ.RCVTIMEO, config.Timeout);
          subSock.Connect($"{config.Publish}"); // Connect to the Publish endpoint which is bound to server

          // poll in a loop (unless told to shutdown)
          while (!token.IsCancellationRequested) 
          { 
            try
            {
              ctlSock.SendAll(HereMessage());

              var reply = ServerMessage.Decode(ctlSock.RecvAll());
              if (reply is ServerMessage.List msg) { userStream_.OnNext(msg); }

              // Add the code needed to receive msg on the channel
              var notice = ServerMessage.Decode(subSock.RecvAll()); // RecvAll method of socket
              if (notice is ServerMessage.News news) { newsStream_.OnNext(news); } // Pass it to utility function which decodes & validates the msg; if valid, pass it to subject and push it to UI thread
            }
            catch (TimeoutException) 
            { 
              //NOTE: A more sophisticated approach might use an "incremental back-off"
              //      strategy for detecting a permanently dead server.
              WriteLine("Timeout while waiting for reply."); 
            }
          }
        }
      }
      , token);
    
    /// <summary>
    /// Observabl stream of conneted users (i.e. periodic snapshots of server state)
    /// </summary>
    public static IObservable<ServerMessage.List> UserStream { get => userStream_; }

    public static IObservable<ServerMessage.News> NewsStream { get => newsStream_; } // Make subscription publicly available, exposing new subject as static readonly property

    /// <summary>
    /// Various settings used to turn ZMQ client behavior
    /// </summary>
    public sealed class Config
    { 
      public string Control { get; }
      public string Publish { get; }
      public int    Timeout { get; }
      public string Handle  { get; }

      public Config(string control, string publish, int timeout, string handle)
      { 
        Control = control;
        Publish = publish;
        Timeout = timeout;
        Handle  = handle;
      }
    }
  }
}
