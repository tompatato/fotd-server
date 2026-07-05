# Client Architecture

Face of Mankind is built on the **LithTech Jupiter** engine, and the client
follows LithTech's standard module split. Three binaries make up the client:

| Binary | Role | Image base |
| --- | --- | --- |
| `fom_client.exe` | LithTech engine host (rendering, input, networking, object system) | `0x00400000` |
| `CShell.dll` | **Client shell** — UI, HUD, menus, and client-side game presentation | `0x10000000` |
| `Object.lto` | **Object module** — gameplay object logic loaded by the engine | `0x10000000` |

The `cshell.dll` + `object.lto` pairing is the canonical LithTech layout. The
engine is confirmed by the globals the game code calls through:

- `FOM::Globals::g_pLTClient` — the `ILTClient` engine interface. Object
  position and rotation in [[Player Update Flow]] come from its vtable
  (`field37_0x94`, `field38_0x98`).
- `FOM::Globals::g_pLTNetwork` — the engine network/player-data interface
  (`GetPlayerData(PLAYERDATA_AVATAR)` supplies the local avatar block).

## How the modules share state

The gameplay/engine side and the [[SharedMemory|CShell UI]] are decoupled
through a single named shared-memory blackboard, **[[SharedMemory]]**. Rather
than calling each other directly, modules publish/read well-known fields (player
id, world id, active weapon, login state, …) by slot index. This is the first
thing to understand before reading any other client note — almost every piece of
game state is sourced from there.

## Networking

RakNet 3.611 underlies the wire protocol (the same library the server vendors in
[`extern/raknet`](../../extern/raknet)). The client packs game state into RakNet
`BitStream`s and sends typed packets (e.g. `ID_UPDATE`) to the master and world
servers. See [[Player Update Flow]] for the concrete path and the **server**
vault for where those packets are handled.
