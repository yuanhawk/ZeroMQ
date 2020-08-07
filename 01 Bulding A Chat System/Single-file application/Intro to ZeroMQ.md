# Overview of ZeroMQ

**Sockets in ZeroMQ does not refer to a traditional socket*

ZeroMQ is a community, a collection of protocols, a collection of Libraries <br>
> Library
> * Mailing List
> * Z Guide

> Protocols
> * ZMTP
> * TSP
> * Z85
> * ZAP
> * CLISRV
> * FILEMQ

> Library
> * libzmq
> * fszmq
> * pyzmq
> * JeroMQ
> * NetMQ
> * chumak

Focus on ZMTP, libzmq, fszmq

Where is ZeroMQ used?
> * Peer Mesh Networks
> * File Sharing
> * Distributed Caches
> * Microservices
> * Web Servers
> * Virtual Infrastructure Management
> * Life Indexing Systems 
> * Log Aggregation
> * Chat Systems
> * Big Data Processing (eg: MapReduce)
> * Data Streaming (eg: Stock Tickers) <br> <br>
> .Net implementation
> * Polyglot Scientific Notebooks
> * Event Sourcing Platform
> * Remote Execution (i.e. Job Systems)

# Primary Concepts
ZMQ Concepts <br>
*A context that collects sockets that exchange messages*
* Context - ZMQ housekeeping, tracks process lifetime, each context is a separate application, manage sockets
* Socket - Create, configure and connect, build distributed systems, tunable set of behavior of connecting, exchange messages
* Message - Frames of binary num
* Transport - Abstracts transport processes, eg: In-Process, TCP, Norm, EPGM, Infrastructure planning

Socket Roles -> Framing -> Protocols 