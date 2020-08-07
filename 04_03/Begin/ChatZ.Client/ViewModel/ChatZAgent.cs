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
  public static class Proxy
  {
    private static readonly Subject<ServerMessage.List> userStream_ = new Subject<ServerMessage.List>();
    private static readonly Subject<ServerMessage.News> newsStream_ = new Subject<ServerMessage.News>();

    public static Task Start(Config config, CancellationToken token) 
      => Task.Run(() => 
      {
        using (var context = new Context())
        { 
          // configure control socket
          var ctlSock = context.Dealer();
          ctlSock.SetOption(ZMQ.IDENTITY, config.Handle);
          ctlSock.SetOption(ZMQ.RCVTIMEO, config.Timeout);
          ctlSock.Connect($"{config.Control}");

          // configure subscriber socket
          var subSock = context.Sub();
          subSock.Subscribe(new[] { GroupTopic });
          subSock.SetOption(ZMQ.RCVTIMEO, config.Timeout);
          subSock.Connect($"{config.Publish}");

          // poll in a loop (unless told to shutdown)
          while (!token.IsCancellationRequested) 
          { 
            try
            {
              ctlSock.SendAll(HereMessage());

              var reply = ServerMessage.Decode(ctlSock.RecvAll());
              if (reply is ServerMessage.List msg) { userStream_.OnNext(msg); }

              var notice = ServerMessage.Decode(subSock.RecvAll());
              if (notice is ServerMessage.News news) { newsStream_.OnNext(news); }
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

    public static IObservable<ServerMessage.News> NewsStream { get => newsStream_; }

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
