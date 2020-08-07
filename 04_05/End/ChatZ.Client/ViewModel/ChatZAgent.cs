using ChatZ.Common;
using fszmq;
using System;
using System.Collections.Concurrent;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

using static ChatZ.Common.Protocol;
using static System.Diagnostics.Trace;

namespace ChatZ.Client
{
  public static class Proxy
  {
    private static readonly BlockingCollection<(string, string)> inbox = new BlockingCollection<(string, string)>();

    private static readonly Subject<ServerMessage.List> userStream_ = new Subject<ServerMessage.List>();
    private static readonly Subject<ServerMessage.News> newsStream_ = new Subject<ServerMessage.News>();

    public static Task Start(Config config, CancellationToken token) 
      => Task.Run(() => 
      {
        var inboxWait = TimeSpan.FromMilliseconds(config.Timeout / 2);

        using (var context = new Context())
        { 
          // configure control socket
          var ctlSock = context.Dealer();
          ctlSock.SetOption(ZMQ.IDENTITY, config.Handle);
          ctlSock.Connect($"{config.Control}");

          // configure subscriber socket
          var subSock = context.Sub();
          subSock.Subscribe(new[] { GroupTopic });
          subSock.Connect($"{config.Publish}");

          var sockets = new[]
          {
            subSock.AsPollIn(sock => {
              if (ServerMessage.Decode(sock.RecvAll()) is ServerMessage.News news)
              {
                newsStream_.OnNext(news);
              }
            }),
            ctlSock.AsPollIn(sock => {
              if (ServerMessage.Decode(sock.RecvAll()) is ServerMessage.List users)
              {
                userStream_.OnNext(users);
              }
            }),
            ctlSock.AsPollOut(sock => {
              var hasTalk = inbox.TryTake(out (string target, string details) msg, inboxWait);
              sock.SendAll(hasTalk ? TalkMessage(msg.details, msg.target) : HereMessage());
            })
          };

          // poll in a loop (unless told to shutdown)
          while (!token.IsCancellationRequested) 
          {
            var didFire = PollingModule.DoPoll(config.Timeout, sockets);
            if (!didFire)
            {
              WriteLine("WARN: no callbacks were triggered");
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

    internal static bool Transmit(string input)
    {
      try
      {
        return inbox.TryAdd((GroupSender, input));
      }
      catch (Exception ex)
      {
        WriteLine(ex.Message);
        return false;
      }
    }
  }
}
