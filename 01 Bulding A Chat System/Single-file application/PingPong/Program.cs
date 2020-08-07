using fszmq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static System.Console;

namespace PingPong // Server End Client
{
  public static class Program
  {
    private const string ENDPOINT = @"tcp://127.0.0.1:2200";
    
    private static readonly byte[] PING = new byte[]{ 0x50, 0x49, 0x4E, 0x47, };
    private static readonly byte[] PONG = new byte[]{ 0x50, 0x4F, 0x4E, 0x47, };

    private static void Client(int identifier, CancellationToken token)
    {
      using (var context = new Context()) // Client function init, new Context
      {
        var socket = context.Req(); // Create new socket
        socket.Connect(ENDPOINT); // Configure as needed

        while (!token.IsCancellationRequested)
        {
          socket.Send(PING); // Send simple Ping msg to the server

          var msg = socket.Recv(); // block waiting to receive a response fr the server
          if (PONG.SequenceEqual(msg)) // Perform minimal validation
          {
            WriteLine($"({identifier}) got ping"); // Write msg to the console
          }
        }
      }
    }

    private static void Server(CancellationToken token)
    {
      using (var context = new Context()) // new zmq context
      {
        var socket = context.Rep(); // create wrap socket fr context
        socket.Bind(ENDPOINT);
        while (!token.IsCancellationRequested)
        {
          var msg = socket.Recv(); // Receive before sending
          if (PING.SequenceEqual(msg))
          {
            Thread.Sleep(1000); // Sleep thread for 1 sec

            socket.Send(PONG); // Send reply to client
          }
        }
      }
    }

    public static void Main(string[] args)
    { // Creates CancellationTokenSource & wires it up to CancelKeyPress event, lets user interrupt running programme
      using (var cts = new CancellationTokenSource())
      { 
        CancelKeyPress += (_, e) => 
        { 
          e.Cancel = true; 
          cts.Cancel();
        };

        foreach (var i in Enumerable.Range(1,5))
        { // 5 separate instances of client each as background task, and given unique identifier for debugging
          // spawn client
          Task.Run(() => Client(i, cts.Token)); 
        }

        // spawn server, launch server as background task
        Task.Run(() => Server(cts.Token)).Wait();
      }
    }
  }
}
