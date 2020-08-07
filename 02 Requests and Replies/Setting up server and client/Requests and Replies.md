# Sync vs async two-way messaging
Socket Roles: Bidirectional

**Channel = Bind + Connect**<br>
2 sockets must establish a connection. If 2 sockets bind w/o connection, there will be no exchange of msg. <br>
Bind the stable peer (relay server), bind to more than 1 endpoint.

**Synchronous or asynchronous**<br>
Message exchange pattern, sockets exchange messages <br>
Synchronous - Cadences is strictly alternating pattern of send/receive or the other way round <br>
Asynchronous - Inbound or outbound, more robust and flexible, complex messaging format

**Socket identity**<br>
Enables socket to identify msg fr a particular sender <br>
ZMQ will stack identities in front of the msg, preserving order of transmission through any number of intermediary sockets (routing envelope) <br>
Keeps tracks of clients coming in and going out

**4 Bidirectional sockets** <br>
Synchronous Peers<br>
REQ <--> REP<br>
Asynchronous Peers<br>
Router <--> Dealer<br>
*Complex msg flow and tracks client presence*<br>
Synchronous + Asynchronous Peers <br>
REQ <--> Router <--> Dealer <--> REP

# Message Framing
* Frames of a multipart msg as a separator
* Same msg can appear differently depending on socket role inspecting msg
* A multipart message with 3 frames of content

Actual Message Content ... ... (As seen by REQ or REP)<br>
(Empty Frame) Actual Message Content ... ... (As seen by DEALER)<br>
(Peer Identity + Empty Frame *) Actual Message Content ... ... (As seen by ROUTER) <br>
*indicates split btw routing envelope & msg content

Keeping Track of Clients <br>
Client (DEALER) Request, Server (ROUTER) Reply

Exercise 1a: Setting up the server <br>
ChatZ.Server --> Program.cs <br>

Exercise 1b: Setting up the client <br>
ChatZ.Client --> ViewModel --> ChatZAgent.cs

**System.BadImageFormatException**
Right Click Solution Node --> Configuration Manager (VS) / Properties (Rider)