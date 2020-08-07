using System.IO;
using System.Linq;
using System.Text;

using date  = System.DateTimeOffset;
using users = System.Collections.Generic.IEnumerable<string>;

using static ChatZ.Common.Library;

namespace ChatZ.Common
{
  /// <summary>
  /// Provides common functionality for the CHAT protocol (framing, encoding, et cetera)
  /// </summary>
  /// <remarks>
  /// This protocol connects many clients to a single server. Once connected, a client can broadcast public messages, 
  /// which are seen by all other connected clients. Additionally, a client can send a private message exclusively to 
  /// a single connected user. The server keeps track of connected clients and relays messages between clients. When
  /// a client appears inactive for a certain period of time, the server "disconnects" that client. Thus, when idle 
  /// (i.e. not sending a TALK message), a client should send HERE messages. Note that message flows between clients 
  /// and server are fully asynchonous and may be unidirectional or bidirectional.
  /// <code>
  /// ; typical message exchanges between client (C) and server (S)
  /// C:HERE (S:LIST) ; idle message
  /// C:TALK          ; send message to individual or group
  /// S:NEWS          ; broadcast message to group or individual
  /// </code>
  /// The framing of each message is given by the following ABNF grammar, where each ZMQ message frame is given a single line.
  /// Please note: the routing envelop and delimiter of ROUTER/DEALER exchanges is omitted from this grammar.
  /// <code>
  /// HERE  = signature protocol version 0x01
  /// 
  /// TALK  = signature protocol version 0x03
  ///         json { "target" : &lt;handle&gt;, "details" : "string" }
  /// 
  /// LIST  = signature protocol version 0x02
  ///         json { "timestamp" : "2017-01-01T00:00:00.000Z", "handles" : [ &lt;handle&gt; ] }
  /// 
  /// NEWS  = signature protocol version topic
  ///         json { "timestamp" : "2017-01-01T00:00:00.000Z", "sender" : &lt;handle&gt;, "details" : "string" }
  /// 
  /// signature = 0x4D 0x4F
  /// protocol  = 0x43 0x48 0x41 0x54
  /// version   = 0x01
  /// 
  /// topic   = client / "$CHATZSRV"
  /// client  = 3*UTF8  ; handle as given via socket identity
  /// </code>
  /// </remarks>
  public static partial class Protocol
  {
    /// <summary>
    /// Indicates the protocol version supported by this library
    /// </summary>
    public const byte Version = 0x01;

    /// <summary>
    /// A literal value used by TALK and NEWS messages to signal a public message
    /// </summary>
    public const string GroupSender = "$CHATZSRV";

    /// <summary>
    /// The binary prefix for public NEWS messages (can be used by subscribers)
    /// </summary>
    public static readonly byte[] GroupTopic = new byte[]
    {
      0x4D, 0x4F, 0x54, 0x41, 0x48, 0x43, Version,
      0x24, 0x43, 0x48, 0x41, 0x54, 0x5A, 0x53, 0x52, 0x56
    };

    /// <summary>
    /// A 7-byte prefix used to disambiguate this versio of this protocol from other messages
    /// </summary>
    public static readonly byte[] Preamble = new byte[] { 0x4D, 0x4F, 0x54, 0x41, 0x48, 0x43, Version };

    /// <summary>
    /// The binary control frame for HERE messages
    /// </summary>
    public static readonly byte[] HereFrame = new byte[] { 0x4D, 0x4F, 0x54, 0x41, 0x48, 0x43, Version, 0x01 };
    
    /// <summary>
    /// The binary control frame for LIST messages
    /// </summary>
    public static readonly byte[] ListFrame = new byte[] { 0x4D, 0x4F, 0x54, 0x41, 0x48, 0x43, Version, 0x02 };

    /// <summary>
    /// The binary control frame for TALK messages
    /// </summary>
    public static readonly byte[] TalkFrame = new byte[] { 0x4D, 0x4F, 0x54, 0x41, 0x48, 0x43, Version, 0x03 };

    /// <summary>
    /// Constructs a new binary prefix for NEWS messages, composed of the <see cref="Preamble"/> and a given topic
    /// <para> This function should only be used for private messages. 
    /// Public broadcasts should make use of the <see cref="GroupTopic"/> frame.</para>
    /// </summary>
    /// <param name="topic">Message classifier used in subscription-based message routing</param>
    /// <returns>A single ZMQ message frame</returns>
    public static byte[] NewsFrame(string topic)
    {
      using (var stream = new MemoryStream())
      using (var writer = new BinaryWriter(stream))
      {
        writer.Write(Preamble);
        writer.Write(Encoding.UTF8.GetBytes(topic));
        return stream.ToArray();
      }
    }

    /// <summary>
    /// Constructs a new binary LIST reply (used by a server when heartbeating)
    /// </summary>
    /// <param name="target">The identity of the client to which the message will be sent</param>
    /// <param name="handles">The screen names of all currently connected clients</param>
    /// <returns>A single ZMQ message consisting of four frames: routing envelop, delimiter, control frame, and payload</returns>
    public static byte[][] ListMessage(byte[] target, users handles)
      => new[] { target, new byte[0], ListFrame, Encode(new { stamp = date.UtcNow, handles = handles.ToArray() }) };

    /// <summary>
    /// Constructs a new binary NEWS message
    /// </summary>
    /// <param name="sender">The screen name of the client sharing the messge</param>
    /// <param name="details">The actual message content being shared</param>
    /// <param name="topic">Should be <see cref="GroupSender"/> for public messages or a screen name for private messages</param>
    /// <returns>A single ZMQ message consisting of two frames: routing prefix and payload</returns>
    public static byte[][] NewsMessage(string sender, string details, string topic = GroupSender)
      => new[] { NewsFrame(topic), Encode(new { stamp = date.UtcNow, sender = sender, details = details }) };
    
    /// <summary>
    /// Constructs a new binary HERE request (used by a client when heartbeating)
    /// </summary>
    /// <returns>A single ZMQ message consisting of two frames: delimiter and control frame</returns>
    public static byte[][] HereMessage()
      => new[] { new byte[0], HereFrame };

    /// <summary>
    /// Constructs a new binary TALK request
    /// </summary>
    /// <param name="details">The content to be shared</param>
    /// <param name="target">
    /// The screen name of the client to which the message should be sent (intended for private messages).
    /// If omitted, message will be public (i.e. sent to all connected clients).
    /// </param>
    /// <returns>A single ZMQ message consisting of three frames: delimiter, control frame, and payload</returns>
    public static byte[][] TalkMessage(string details, string target = GroupSender)
      => new[] { new byte[0], TalkFrame, Encode(new { target = target, details = details }) };
  }
}
