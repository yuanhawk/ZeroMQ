using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using date  = System.DateTimeOffset;
using users = System.Collections.Generic.IEnumerable<string>;

using static ChatZ.Common.Library;
using static ChatZ.Common.Protocol;

namespace ChatZ.Common
{
  /// <summary>
  /// Encodes all the possible variants of a reply a client might receiver from the server
  /// <para>(Note: instances shoulde be created via the class-level <see cref="Decode"/> method.)</para>
  /// </summary>
  public abstract class ServerMessage
    : IEquatable<ServerMessage>
    , IStructuralEquatable
    , IComparable
    , IComparable<ServerMessage>
    , IStructuralComparable
  {
    private readonly int tag;

    internal ServerMessage(int tag) { this.tag = tag; }

    public int CompareTo(object other, IComparer comparer)
    {
      if (other is null) { return 1; }

      if (other is ServerMessage that) 
      { 
        if (this.tag == that.tag) 
        { 
          switch (this.tag)
          {
            case 1:
              var l1 = (List) this;
              var l2 = (List) that;
              return comparer.Compare(l1.Stamp, l2.Stamp);

            case 2:
              var n1 = (News) this;
              var n2 = (News) that;
              var res = comparer.Compare(n1.Stamp, n2.Stamp);
              if (res == 0) { res = comparer.Compare(n1.Sender, n2.Sender); }
              if (res == 0) { res = comparer.Compare(n1.Detail, n2.Detail); }
              return res;

            default:
              return 0;
          }
        }

        return (this.tag > that.tag) ? 1 : -1;
      }
      
      return -1;
    }

    public int CompareTo(ServerMessage other) 
      => CompareTo(other, StructuralComparisons.StructuralComparer);

    public int CompareTo(object obj) 
      => CompareTo(obj, StructuralComparisons.StructuralComparer);

    public int GetHashCode(IEqualityComparer comparer)
    {
      switch (this.tag)
      {
        case 1:
          var listMsg = (List) this;
          return HashCombine(
            this.tag,
            HashCombine(
              comparer.GetHashCode(listMsg.Stamp),
              comparer.GetHashCode(listMsg.Users)
            )
          );

        case 2:
          var newsMsg = (News) this;
          return HashCombine(
            this.tag,
            HashCombine(
              comparer.GetHashCode(newsMsg.Stamp),
              HashCombine(
                newsMsg.Sender.GetHashCode(),
                newsMsg.Detail.GetHashCode()
              )
            )
          );
      }
      return 0;
    }

    public override int GetHashCode()
      => GetHashCode(StructuralComparisons.StructuralEqualityComparer);

    public bool Equals(object other, IEqualityComparer comparer)
    {
      if (other is ServerMessage that && this.tag == that.tag)
      {
        switch (this.tag)
        {
          case 1:
            var l1 = (List) this;
            var l2 = (List) that;
            return comparer.Equals(l1.Stamp, l2.Stamp)
                && l1.Users.SequenceEqual(l2.Users, EqualityComparer<string>.Default);

          case 2:
            var n1 = (News) this;
            var n2 = (News) that;
            return comparer.Equals(n1.Stamp, n2.Stamp)
                && string.Equals(n1.Sender, n2.Sender)
                && string.Equals(n1.Detail, n2.Detail);

          default:
            return true;
        }
      }
      return false;
    }

    public bool Equals(ServerMessage other)
      => Equals(other, StructuralComparisons.StructuralEqualityComparer);

    public override bool Equals(object obj)
      => Equals(obj, StructuralComparisons.StructuralEqualityComparer);

    public override string ToString()
    {
      switch (this.tag)
      {
        case 1:
          var listMsg = (List) this;
          return $"{nameof(List)} ({listMsg.Stamp}, [{listMsg.Users.Count()} {nameof(listMsg.Users)}])";

        case 2:
          var newsMsg = (News) this;
          return $"{nameof(News)} ({newsMsg.Stamp}, '{newsMsg.Sender}', '{newsMsg.Detail}')";

        default:
          return $"{nameof(None)}";
      }
    }

    /// <summary>
    /// Executes the appropriate callback based on the variant of the <see cref="ServerMessage"/> instance
    /// </summary>
    /// <typeparam name="TResult">The type of data to be returned from any callback</typeparam>
    /// <param name="list">The <see cref="Func{T,R}"/> to be executed for a LIST reply</param>
    /// <param name="news">The <see cref="Func{T,R}"/> to be executed for a NEWS broadcast</param>
    /// <param name="none">The <see cref="Func{R}"/> to be executed for an invalid message</param>
    /// <returns>The result of invoking this appropriate callback</returns>
    public TResult Match<TResult>(Func<List, TResult> list, Func<News, TResult> news, Func<TResult> none)
    {
      switch (this.tag)
      {
        case 1: return list((List) this);
        case 2: return news((News) this);

        default: return none();
      }
    }

    /// <summary>
    /// Executes the appropriate callback based on the variant of the <see cref="ServerMessage"/> instance
    /// </summary>
    /// <param name="list">The <see cref="Action{T}"/> to be executed for a LIST reply</param>
    /// <param name="news">The <see cref="Action{T}"/> to be executed for a NEWS broadcast</param>
    /// <param name="none">The <see cref="Action"/> to be executed for an invalid message</param>
    public void Match(Action<List> list, Action<News> news, Action none)
    {
      switch (this.tag)
      {
        case 1: list((List) this); break;
        case 2: news((News) this); break;

        default: none(); break;
      }
    }
    
    /// <summary>
    /// Indicates that a malformed (or otherwise invalid) message was received, or that no message has been received
    /// </summary>
    private sealed class None_ : ServerMessage
    {
      internal None_() : base(0) { }
    }

    /// <summary>
    /// Contains the details of a LIST reply
    /// </summary>
    public sealed class List : ServerMessage
    {
      internal List(date stamp, string[] users) : base(1)
      {
        Stamp = stamp;
        Users = users ?? new string[0];
      }

      /// <summary>
      /// The point-in-time, in UTC, when the server sent the message
      /// </summary>
      public date Stamp { get; }

      /// <summary>
      /// The handles of all the clients connected to the server (at time of message transmission)
      /// </summary>
      public users Users { get; }
    }

    /// <summary>
    /// Contains the details of a NEWS broadcast
    /// </summary>
    public sealed class News : ServerMessage
    {
      internal News(string topic, date stamp, string sender, string detail) : base(2)
      {
        Topic = topic;
        Stamp = stamp;
        Sender = sender ?? "";
        Detail = detail ?? "";
      }

      /// <summary>
      /// The intended recipient of the broadcast
      /// </summary>
      public string Topic { get; }

      /// <summary>
      /// The point-in-time, in UTC, when the server sent the message
      /// </summary>
      public date Stamp { get; }

      /// <summary>
      /// The client (or server) who originated the broadcast
      /// </summary>
      public string Sender { get; }

      /// <summary>
      /// The actual content of the broadcast
      /// </summary>
      public string Detail { get; }
    }

    /// <summary>
    /// Indicates that a malformed (or otherwise invalid) message was received, or that no message has been received
    /// </summary>
    public static readonly ServerMessage None = new None_();

    /// <summary>
    /// Translates a binary ZMQ message into the appropriate sub-class of <see cref="ServerMessage"/>
    /// </summary>
    /// <param name="message">A single ZMQ message consisting of 1 or more frames of 0 or more bytes</param>
    /// <returns>An instance of a sub-class of <see cref="ServerMessage"/></returns>
    public static ServerMessage Decode(byte[][] message)
    {
      var list = new { stamp = date.MinValue, handles = new string[0] };
      var news = new { stamp = date.MinValue, sender = "", details = "" };

      switch (message?.Length)
      {
        case 3
        when message[0]?.Length == 0
          && ListFrame.SequenceEqual(message[1])
          && TryDecode(message[2], ref list):
          return new List(list.stamp, list.handles);

        case 2
        when Preamble.SequenceEqual(message[0]?.Take(7))
          && TryGetString(message[0].Skip(7).ToArray(), out string topic)
          && TryDecode(message[1], ref news):
          return new News(topic, news.stamp, news.sender, news.details);

        default:
          return None;
      }
    }
  }
}
