# Broadcast Messaging
### Broadcast model (PUB SUB)
Information flows in a single direction<br>
Publisher bind and send messages regardless of whether there are receivers<br>
Unidirection flow, more content, faster<br>
Subscribers connect and receive messages<br>
Filtered by "topic" (prefix match), leading bytes of message

### Socket Roles: Broadcast<br>
PUB role<br>
Publiser behavior, does NOT receive, fair and efficient distribution to all connected subscribers<br>
SUB role<br>
Consuming broadcast,does NOT send, MUST set >=1 subscription

One publisher --> Many subscribers<br>
Publisher stable nodes, subscriber transient node<br>
Many publisher --> Many subscribers<br>
XSUB + XPUB (sets of publishers and subscriber)<br>
Similar to PUB/SUB sockets, but pair subscription info as normal msg<br>
Filter info<br>
SUB --> XPUB --> SUB --> PUB

# Socket Roles: Topology
Subscribers can set multiple subscription, filtering via binary prefix matching<br>
Publisher Topics: antique, antelopes, and, an, ant<br>
Subscriber Filters: ant<br>
Matched Topics: antique, antelopes, ant<br>
PUB socket to server, and broadcast msg with 2 frames, 1 msg frame as topic, 2nd frame as content<br>
SUB socket to receive updates<br>
First few bytes to describe chat protocol, set subscription that matches full topic frame