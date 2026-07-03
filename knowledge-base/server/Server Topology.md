# Server Topology

Two server roles plus a database, all over RakNet/UDP. Ports are defined in
[`server-shared/Core/Constants/ServerConstants.cs`](../../server-shared/Core/Constants/ServerConstants.cs).

| Process | Listens | For |
| --- | --- | --- |
| Master server | `61000/udp` (`MasterClientPort`) | client login / world directory |
| Master server | `61100/udp` (`MasterWorldPort`) | world-server registration |
| World server *N* | `61000 + N` /udp (`GetWorldClientPort`) | in-world clients |
| Database | MariaDB (`3306`; host-mapped `33066`) | persistence |

World server 1 (Manhattan) therefore listens on `61001/udp`. The client connects
to the master with `+MasterServer <host>` (default port 61000).

## Login & world handoff

1. Client connects to the master (`61000`). `LoginHandler` looks up the account
   by username.
   - **No account →** `InvalidLogin`.
   - **Account, no character →** `CreateCharacter` (client opens character
     creation).
   - **Account + character →** `Success`, with a `LoginWorldId`.
   - ⚠️ Password verification is currently a **TODO** in
     [`master-server/Application/Handlers/LoginHandler.cs`](../../master-server/Application/Handlers/LoginHandler.cs)
     — any password is accepted for an existing account. Account rows store a
     hash (`MD5(MD5(password)_hex + username)`), so the check can be wired in
     later without a schema change.
2. World servers register with the master on `61100`; the master then advertises
   each world to clients as "ready" at `clientHost:6100N`.
3. The client connects to the assigned world server and begins sending updates.

> Deploy note: the world server reads its client-facing host from
> `Server:ClientHost` (it refuses to start if blank). The compose env var is
> `Server__ClientHost`.

> ⚠️ **Logout is incomplete.** On leaving a world the world server forwards a
> `PlayerMigrateWorld` to the master, but `PlayerMigrateWorldHandler` only handles
> migration to a *pending destination world* — there is no return-to-login path, so
> a client that chooses "log out" hangs on "Logging Out". A hard disconnect
> (`ConnectionLost`/`Disconnection`) tears the session down cleanly instead.

## Receiving the client's state — `ID_UPDATE`

`ID_UPDATE` (= 126) carries the player's `WorldUpdate` (the client's *Player
Update Flow*, documented in the **client** vault). Handled in
[`world-server/Application/Handlers/UpdateHandler.cs`](../../world-server/Application/Handlers/UpdateHandler.cs):

- resolve the player by sender address (warn + drop if unregistered),
- require `WorldUpdate.Kind == Player` (the client only sends player updates),
- `player.ApplyUpdate(update.Player)`,
- `PlayerUpdateService.QueueUpdate(player)` to fan the new state out to others.

The managed `WorldUpdate`
([`server-shared/Core/Packets/Types/WorldUpdate.cs`](../../server-shared/Core/Packets/Types/WorldUpdate.cs))
is the deserialized mirror of the client's structure documented in the client
vault; its `Type { Invalid, Player, Character }` matches the client's leading
`type` byte.
