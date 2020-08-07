# Combining Channels
### Combining Sockets: Proxies
**Complex networks commonly use intermediaries**<br>
**Effectively "glue" 2 sockets together**<br>
Overall topology of distributed apps, many transient nodes connecting to stable nodes<br>
Stable nodes are simple intermediaries which shuffle data fr 1 set of concerns to another<br>
2 calls which simplifies the process<br>
**ZMQ has "built-in" support**
Proxy & Steerable proxy take 2 sockets as input, arranged such that msg flows btw them.<br>
Proxy is expected to run until app shutdown, steerable proxy can be stopped and started as needed.
In either case, both sockets need to be completely configured (bind/connect) before proxy is invoked<br>
Both sockets must belong to the same Context.

Proxy shuffles msgs btw sockets, depending on socket roles, different msg flow behavior will emerge.<br>
Router + Dealer --> Queue <br>
Requests of many clients are serviced by pool of available of workers. With controlled pool of wrap sockets, each does discrete work at the behast of the client<br>
XPub + XSub --> Forwarder <br>
Connect multiple publishers to multiple subscribers, useful for logical separated subnetworks which use different transports.<br>
Eg: Pub Sockets broadcast over TCP and forwarded to subscribers listening via EPGM, forwarder as bridged the 2 different transports

# Combining Sockets: Polling
**Reactive callback-driven processing**<br>
ZMQ manages timing<br>
Schedule callbacks and lets ZMQ handle the invocation<br>
* More fair & efficient
* Subtle & profound impact on code
* Reactive rather than procedural<br>

**Handle inbound and/or outbound messages**
Depending on socket role, handle traffic with polling. Handle both with a callback or dedicated callback for each direction
Polling supports a configurable timeout.

**Integrate with OS file descriptors**<br>
Makes ZMQ sockets with OS file descriptors, beneficial in unmanaged or low latency env, and in situation where ZMQ to integrate with existing msg prompt.<br>

### Sequential Sockets
set RCVTIMEO for socket
trap Timeout Exception
Explicitly invoke sending or receiving operations on sockets
Prioritise dealer socket
If blocked for too long, delays/prevents subsocket fr receiving msg

### Polling Sockets
3 distinct callbacks
* Receiving on dealer socket
* Sending on dealer socket
* Receiving on sub socket

Poll up to a configurable timeout <br>
Delays on 1 socket do not impact other sockets