# Server Knowledge Base

Operational and protocol notes for the emulator's **master** and **world**
servers — the runtime topology, the wire ports, and how the client's packets are
handled. For deep code structure, defer to the source and
[`docs/architecture.md`](../../docs/architecture.md); these notes capture the
runtime/protocol picture and the points where the client (documented in the
**client** vault) meets the server.

## Start here

- [[Server Topology]] — processes, ports, login/world handoff, `ID_UPDATE`

> The reverse-engineered client side lives in the sibling **client** vault.
