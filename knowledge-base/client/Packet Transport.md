# Packet Transport

How the client frames and sends gameplay packets over RakNet. Every typed
packet (e.g. the `ID_UPDATE` carrying a [[WorldUpdate]]) is a
`FOM::Packets::VariableSizedPacket` that writes a small header into an embedded
RakNet `BitStream`, then hands the whole thing to the engine's network
interface via `FOM::SendPacket`.

All addresses below are `CShell.dll` (image base `0x10000000`); the same classes
exist in `Object.lto` and `fom_client.exe`.

## The packet object — `VariableSizedPacket`

`type /FOM/Packets/VariableSizedPacket` — 1072 bytes, packed:

| Offset | Field | Type | Notes |
| --- | --- | --- | --- |
| `0x000` | `vftable` | ptr | `{ dtor, Read, Write }` (3 slots) |
| `0x004` | *(unknown)* | u32 | unverified |
| `0x008` | `timestampType` | `PacketIdentifier` (u8) | set to `ID_TIMESTAMP` to prefix a timestamp |
| `0x00c` | `bitStream` | `RakNet::BitStream` (1044B) | the serialized payload buffer |
| `0x420` | `timestamp` | `RakNetTime` (8B) | filled at write time |
| `0x428` | `messageType` | `PacketIdentifier` (u8) | the packet id, e.g. `ID_UPDATE` (126) |

The embedded 1044-byte `BitStream` (see [[WorldUpdate]] for its layout) is the
heart of it — header and body are both bit-packed into this one buffer. The
class is the base for concrete packets (e.g. `Packet_ID_UPDATE`), which supply
their own vftable and append a body.

## On-wire header — `VariableSizedPacket::Write` (rva `0xc770`)

`Write` emits only the **header** into the BitStream (after `ResetWritePointer`):

1. **If** `timestampType == ID_TIMESTAMP`: write one byte `0x19` (= 25, the
   RakNet `ID_TIMESTAMP` marker) as 8 aligned bits, then capture
   `RakNet::GetTime()` into `timestamp` and write it as a **64-bit** value
   (`BitStream::Write_T_uint64`).
2. Write `messageType` as one byte (8 aligned bits).

So the wire layout is:

```
[ 0x19  +  timestamp(8 bytes) ]?   messageType(1 byte)   <body…>
   └─ present only when timestampType == ID_TIMESTAMP ─┘
```

The **body** is not written by this base `Write`; the concrete packet appends it
to the same `bitStream` afterwards (e.g. `WorldUpdate::Write(&u, &pkt.bitStream)`
in [[Player Update Flow]]). The body's encoding is covered in
[[WorldUpdate Wire Format]].

### Field codecs (helpers on the packet)

`VariableSizedPacket` carries the bit-level codec helpers the bodies use:
`WriteCompressed_ushort`/`_uint` + `ReadCompressed_*` (RakNet variable-length
integer compression), `ReadBit`, and `WriteString`/`ReadString` /
`EncodeString`/`DecodeString`. So bodies favour *compressed* integers and bit
flags rather than fixed-width fields — relevant when decoding payloads.

## Sending — `FOM::SendPacket` (rva `0x18d9c0`)

Signature (recovered):

```c
bool SendPacket(VariableSizedPacket *packet,
                PacketDestination destination,   // 1=MASTER, 2=WORLD
                int priority,                     // RakNet PacketPriority
                int reliability,                  // RakNet PacketReliability
                int orderingChannel);
```

Behaviour:
- No-ops safely if `Globals::g_pLTNetwork` is null or `packet` is null.
- If the BitStream has no bits yet, it calls `packet->vftable->Write()` to
  populate it (so you can send a freshly-built packet without writing it
  manually); if the caller already wrote bits, that is skipped.
- Forwards to the engine: `g_pLTNetwork->SendPacket(packet, priority,
  reliability, orderingChannel, destination)` — note the engine's `ILTNetwork`
  reorders the args, with `destination` last.

### Argument enums

- `PacketDestination` (`type /FOM/Enums/PacketDestination`): `MASTER = 1`,
  `WORLD = 2` — selects which server connection to use.
- `priority` = RakNet `PacketPriority` (`extern/raknet/include/raknet/PacketPriority.h`):
  `SYSTEM=0, HIGH=1, MEDIUM=2, LOW=3`.
- `reliability` = RakNet `PacketReliability`: `UNRELIABLE=0,
  UNRELIABLE_SEQUENCED=1, RELIABLE=2, RELIABLE_ORDERED=3, RELIABLE_SEQUENCED=4`.

### Worked example — the player update

[[Player Update Flow]] sends with `SendPacket(pkt, WORLD, 1, 1, 2)`, i.e.
**destination WORLD, HIGH_PRIORITY, UNRELIABLE_SEQUENCED, ordering channel 2** —
the classic choice for position/state updates: latest-wins, no retransmit, older
out-of-order updates discarded.

## Reproduce

```bash
python3 tools/re/fomre.py type /FOM/Packets/VariableSizedPacket
python3 tools/re/fomre.py decompile "CShell.dll:0x1000c770"   # Write (header)
python3 tools/re/fomre.py decompile "CShell.dll:0x1018d9c0"   # SendPacket
python3 tools/re/fomre.py type /FOM/Enums/PacketDestination
```

See [[Client Architecture]] for the LithTech `ILTNetwork` interface this routes
through.
