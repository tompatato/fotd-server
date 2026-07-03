# World Logout

How the client leaves a world — either logging out to character-select or as a
prelude to switching worlds. The counterpart to [[World Login Handoff]]. The key
surprise: logout is **fire-and-forget** from the client's side. It sends one
notification to the **master** and then waits, not for a reply packet, but for
the **world server to close its world connection**. Nothing else advances it.

## Flow

```
Client  --ID_WORLD_LOGOUT(playerId, isChangingWorlds)-->  Master
                                        Master (isChangingWorlds == 0):
                                          - clears the session's current world
                                          - forwards ID_WORLD_LOGOUT to that world server
                                        World server:
                                          - ends the player's session (persist + remove)
                                          - CloseConnection(client, sendNotification=true)
Client  <== ID_DISCONNECTION_NOTIFICATION (world connection) ==  World
        (world socket drops -> IS_CONNECTED_TO_WORLD clears -> character-select)
```

`ID_WORLD_LOGOUT` = **116**. There is no `ID_WORLD_LOGOUT_RETURN`; the client has
no receive-handler for a logout packet. The connection teardown *is* the ack.

## `Packet_ID_WORLD_LOGOUT` (116)

`type /FOM/Packets/Packet_ID_WORLD_LOGOUT` — body after the
[[Packet Transport|VariableSizedPacket]] base (`+0x430`):

| Off | Field | Type | Notes |
| --- | --- | --- | --- |
| `0x430` | `playerId` | u32 | the logging-out player |
| `0x434` | `isChangingWorlds` | u8 | 1 = switching worlds, 0 = full logout |

On the wire the body is `WriteCompressed(playerId)` followed by a **single bit**
for `isChangingWorlds` (`BitStream::Write0`/`Write1`) — not a compressed byte.

## Client side — `FOM::Game::LogoutFromWorld(char isChangingWorlds)` (Object.lto rva `0x79690`)

Guarded by `SharedMemory::ReadBool(IS_LOGGED_INTO_WORLD)`. When in a world it:

1. Builds `Packet_ID_WORLD_LOGOUT`, writes `playerId`
   (`SharedMemory::ReadUInt(PLAYER_ID)`) + the `isChangingWorlds` bit, and
   `SendPacket(pkt, MASTER, HIGH, RELIABLE_ORDERED, 0)` — note **MASTER**, not the
   world server.
2. Tears down its *local* world state: shows "Leaving world",
   `SharedMemory::WriteByte(IS_LOGGED_INTO_WORLD, 0)`, posts a travel-manager
   message, and returns.

It does **not** close the world RakNet connection itself and does **not** touch
`IS_CONNECTED_TO_WORLD` (slot 140). Callers (`FUN_10079960` cases 5/0xc, the
`0x6f` travel-manager message loop) are a self-contained state machine that never
touches the network again after that one send — so without an external
connection event the client is stuck on the post-logout screen.

### Why it hangs without server support

`IS_LOGGED_INTO_WORLD` (85, cleared locally) and `IS_CONNECTED_TO_WORLD` (140,
managed by the RakNet connection layer) are distinct. The character-select UI is
gated on the world connection actually being down. If the server drops the
unhandled `ID_WORLD_LOGOUT`, the world socket stays up, slot 140 stays 1, and the
client waits forever. A full **quit** works because the client tears down both
connections itself; logout deliberately keeps them open (it wants to stay logged
into master) and waits for the server to end the world session.

## Server side (emulator)

- **Master** `WorldLogoutHandler`: looks up the session by sender address,
  verifies `PlayerId`. For `isChangingWorlds == 1` it does nothing (the switch is
  driven by the follow-up [[World Login Handoff|ID_WORLD_LOGIN]]). For `== 0` it
  clears `session.CurrentWorld` and forwards `ID_WORLD_LOGOUT` to the world server
  hosting the player.
- **World** `WorldLogoutHandler`: closes the client's world connection
  (`FOMNetwork_Server_CloseConnection`, which sends a disconnection notification)
  and logs the player out (persist + remove). The disconnect is the client's cue.
- `ID_WORLD_LOGOUT` is claimed on the world's master-facing network manager so a
  client can't inject it directly into the world's client port (same guard as
  `ID_PLAYER_LEAVING_WORLD`).

## Reproduce

```bash
python3 tools/re/fomre.py type /FOM/Packets/Packet_ID_WORLD_LOGOUT
python3 tools/re/fomre.py decompile "Object.lto:0x10079690"   # LogoutFromWorld
python3 tools/re/fomre.py decompile "Object.lto:0x10079960"   # travel-manager cases
```

See [[World Login Handoff]], [[Packet Transport]] and [[SharedMemory]].
