# Mail Check

The client polls the server for new mail, and — surprisingly — **gates vortex
travel on it**. On world entry (and every ~30s thereafter) the client sends a
mail-check request and, until it receives a reply, refuses to use a vortex with:

> *"The system is currently checking for new mail. Please try to use a vortex
> gate at a later time!"* (string `5313`)

If the server never answers, that gate never lifts — so the mail handshake is a
hard prerequisite for [[Vortex Gates]], even without any mail UI feature.

Packets (`FOM::Packets`):

| Packet | ID | Dir | Purpose |
| --- | --- | --- | --- |
| `ID_CHECK_MAIL` | 139 | Client→**World** | "is there new mail?" (on entry + ~30s poll) |
| `ID_MAIL` | 140 | World→Client | inbox contents / check result |

Both were commented out in `PacketIdentifier` (native + managed) — unimplemented.
`ID_CHECK_MAIL` is **not** received by any client dispatcher, confirming it is
client→server; the client dispatches the reply `ID_MAIL` to `FUN_10193740`.

## Reply wire format (`ID_MAIL`, empty case)

The client's reader is `FUN_1013ddc0` → `FUN_1013dd20`:

```
[ VariableSizedPacket base | playerId : compressed-uint
                           | mailCount : compressed-u8      (FUN_1013dd20)
                           | mailCount × entry (0x848 bytes each)
                           | hasAppendix : 1 bit  (+ FUN_1013db60 when set) ]
```

Each entry is a 0x848-byte record (sender/subject/body strings + a type byte
switch in `FUN_10193740`). Only the **empty inbox** is implemented: a `mailCount`
of `0` skips the entry loop entirely, and a `false` appendix bit closes the
packet — a complete "no new mail" answer that releases the vortex gate.

## Server-side status

Implemented on the world server: `CheckMailHandler` answers `ID_CHECK_MAIL` with
an empty `ID_MAIL` (`{ playerId, mailCount = 0 }`) for the registered player.
Verified live — the world log shows the poll being answered and the vortex gate
releases. The full mailbox (compose / read / send, non-empty inboxes) is future
work; this is only the handshake needed to unblock travel.

## Reproduce

```bash
fomre decompile "CShell.dll:0x10193740"   # ID_MAIL handler (dispatched from FOM::HandlePacket)
fomre decompile "CShell.dll:0x1013ddc0"   # ID_MAIL read (wire format)
fomre decompile "CShell.dll:0x1013dd20"   # mail list: count byte + entries
```

See [[Vortex Gates]] (the feature this unblocks), [[Packet Transport]], and
[[Client Architecture]].
