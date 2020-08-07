using fszmq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.Console;

namespace PingPong
{
  public static class Program
  {
    private const string ENDPOINT = @"tcp://127.0.0.1:2200";
    
    private static readonly byte[] PING = new byte[]{ 0x50, 0x49, 0x4E, 0x47, };
    private static readonly byte[] PONG = new byte[]{ 0x50, 0x4F, 0x4E, 0x47, };

    private static void Client(int identifier, CancellationToken token)
    {
      using (var context = new Context())
      {
        var socket = context.Req();
        socket.Connect(ENDPOINT);

        while (!token.IsCancellationRequested)
        {
          socket.Send(PING);

          var msg = socket.Recv();
          if (PONG.SequenceEqual(msg))
          {
            WriteLine($"({identifier}) got ping");
          }
        }
      }
    }

    private static void Server(CancellationToken token)
    {
      using (var context = new Context())
      {
        var socket = context.Rep();
        socket.Bind(ENDPOINT);

        while (!token.IsCancellationRequested)
        {
          var msg = socket.Recv();
          if (PING.SequenceEqual(msg))
          {
            Thread.Sleep(1000);

            socket.Send(PONG);
          }
        }
      }
    }

    public static void Main(string[] args)
    {
      using (var cts = new CancellationTokenSource())
      { 
        CancelKeyPress += (_, e) => 
        { 
          e.Cancel = true; 
          cts.Cancel();
        };

        foreach (var i in Enumerable.Range(1,5))
        { 
          // spawn client
          Task.Run(() => Client(i, cts.Token)); 
        }

        // spawn server
        Task.Run(() => Server(cts.Token)).Wait();
      }
    }
  }
}
