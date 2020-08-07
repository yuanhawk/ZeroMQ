using System;
using System.Collections;
using System.Linq;

using static ChatZ.Common.Library;
using static ChatZ.Common.Protocol;

namespace ChatZ.Common
{
  /// <summary>
  /// Encodes all the possible variants of a request the server might receiver from a client
  /// <para>(Note: instances shoulde be created via the class-level <see cref="Decode"/> method.)</para>
  /// </summary>
  public abstract class ClientMessage
    : IEquatable<ClientMessage>
    , IStructuralEquatable
    , IComparable
    , IComparable<ClientMessage>
    , IStructuralComparable
  {
    private readonly int tag;

    internal ClientMessage(int tag) { this.tag = tag; }
    
    public int CompareTo(object other, IComparer comparer)
    {
      if (other is null) { return 1; }

      if (other is ClientMessage that) 
      { 
        if (this.tag == that.tag) 
        { 
          switch (this.tag)
          {
            case 1:
              var h1 = (Here) this;
              var h2 = (Here) that;
              return comparer.Compare(h1.Sender, h2.Sender);

            case 2:
              var t1 = (Talk) this; 
              var t2 = (Talk) that;
              var res = comparer.Compare(t1.Target, t2.Target);
              if (res == 0) { res = comparer.Compare(t1.Sender, t2.Sender); }
              if (res == 0) { res = comparer.Compare(t1.Detail, t2.Detail); }
              return res;

            default:
              return 0;
          }
        }

        return (this.tag > that.tag) ? 1 : -1;
      }
      
      return -1;
    }

    public int CompareTo(ClientMessage other) 
      => CompareTo(other, StructuralComparisons.StructuralComparer);

    public int CompareTo(object obj) 
      => CompareTo(obj, StructuralComparisons.StructuralComparer);

    public int GetHashCode(IEqualityComparer comparer)
    {
      switch (this.tag)
      {
        case 1:
          return HashCombine(this.tag, ((Here) this).Sender.GetHashCode());

        case 2:
          var msg = (Talk) this;
          return HashCombine(
            this.tag,
            HashCombine(
              msg.Sender.GetHashCode(),
              HashCombine(
                msg.Target.GetHashCode(),
                msg.Detail.GetHashCode()
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
      if (other is ClientMessage that && this.tag == that.tag)
      {
        switch (this.tag)
        {
          case 1:
            var h1 = (Here) this;
            var h2 = (Here) that;
            return comparer.Equals(h1.Sender, h2.Sender);

          case 2:
            var t1 = (Talk) this; 
            var t2 = (Talk) that;
            return string.Equals(t1.Sender, t2.Sender)
                && string.Equals(t1.Target, t2.Target)
                && string.Equals(t1.Detail, t2.Detail);

          default:
            return true;
        }
      }
      return false;
    }

    public bool Equals(ClientMessage other)
      => Equals(other, StructuralComparisons.StructuralEqualityComparer);

    public override bool Equals(object obj)
      => Equals(obj, StructuralComparisons.StructuralEqualityComparer);

    public override string ToString()
    {
      switch (this.tag)
      {
        case 1:
          return $"{nameof(Here)} ('{((Here) this).Sender}')";

        case 2:
          var msg = (Talk) this;
          return $"{nameof(Talk)} ('{msg.Sender}', '{msg.Target}', '{msg.Detail}')";

        default:
          return $"{nameof(None)}";
      }
    }

    /// <summary>
    /// Executes the appropriate callback based on the variant of the <see cref="ClientMessage"/> instance
    /// </summary>
    /// <typeparam name="TResult">The type of data to be returned from any callback</typeparam>
    /// <param name="here">The <see cref="Func{T,R}"/> to be executed for a HERE message</param>
    /// <param name="talk">The <see cref="Func{T,R}"/> to be executed for a TALK request</param>
    /// <param name="none">The <see cref="Func{R}"/> to be executed for an invalid message</param>
    /// <returns>The result of invoking this appropriate callback</returns>
    public TResult Match<TResult>(Func<Here, TResult> here, Func<Talk, TResult> talk, Func<TResult> none)
    {
      switch (this.tag)
      {
        case 1: return here((Here) this);
        case 2: return talk((Talk) this);

        default: return none();
      }
    }

    /// <summary>
    /// Executes the appropriate callback based on the variant of the <see cref="ClientMessage"/> instance
    /// </summary>
    /// <param name="here">The <see cref="Action{T}"/> to be executed for a HERE message</param>
    /// <param name="talk">The <see cref="Action{T}"/> to be executed for a TALK request</param>
    /// <param name="none">The <see cref="Action"/> to be executed for an invalid message</param>
    public void Match(Action<Here> here, Action<Talk> talk, Action none)
    {
      switch (this.tag)
      {
        case 1: here((Here) this); break;
        case 2: talk((Talk) this); break;

        default: none(); break;
      }
    }
    
    /// <summary>
    /// Indicates that a malformed (or otherwise invalid) message was received, or that no message has been received
    /// </summary>
    private sealed class None_ : ClientMessage
    {
      internal None_() : base(0) { }
    }

    /// <summary>
    /// Contains the details of a HERE message
    /// </summary>
    public sealed class Here : ClientMessage
    {
      internal Here(string sender) : base(1)
      {
        Sender = sender;
      }

      /// <summary>
      /// The handle (i.e. identity) of the sending client
      /// </summary>
      public string Sender { get; }
    }

    /// <summary>
    /// Contains the details of a TALK request
    /// </summary>
    public sealed class Talk : ClientMessage
    {
      internal Talk(string sender, string target, string detail) : base(2)
      {
        Target = target;
        Sender = sender;
        Detail = detail;
      }

      /// <summary>
      /// The handle with which the message should be shared
      /// </summary>
      public string Target { get; }

      /// <summary>
      /// The handle of the sending client
      /// </summary>
      public string Sender { get; }

      /// <summary>
      /// The actual content to be shared
      /// </summary>
      public string Detail { get; }
    }

    /// <summary>
    /// Indicates that a malformed (or otherwise invalid) message was received, or that no message has been received
    /// </summary>
    public static readonly ClientMessage None = new None_();

    /// <summary>
    /// Translates a binary ZMQ message into the appropriate sub-class of <see cref="ClientMessage"/>
    /// </summary>
    /// <param name="message">A single ZMQ message consisting of 1 or more frames of 0 or more bytes</param>
    /// <returns>An instance of a sub-class of <see cref="ClientMessage"/></returns>
    public static ClientMessage Decode(byte[][] message)
    {
      var talk = new { target = "", details = "" };

      switch (message?.Length)
      {
        case 4
        when TryGetString(message[0], out string sender)
          && message[1]?.Length == 0
          && TalkFrame.SequenceEqual(message[2])
          && TryDecode(message[3], ref talk):
          return new Talk(sender, talk.target, talk.details);

        case 3
        when TryGetString(message[0], out string sender)
          && message[1]?.Length == 0
          && HereFrame.SequenceEqual(message[2]):
          return new Here(sender);

        default:
          return None;
      }
    }
  }
}