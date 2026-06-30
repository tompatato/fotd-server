# Login Handshake

How the client authenticates with the **master server**. It's a two-round
handshake — a lightweight request (username + version) followed by the full
credential packet — defined in `fom_client.exe` under `FOM::Packets`. The
client-side password hash is **identical** to the scheme the server stores, so
authentication will work unchanged once the server wires up its `Check Password`
TODO. See [[Client Architecture]] for the engine/module split.

## Flow

```
Client  --ID_LOGIN_REQUEST(username, clientVersion)-->  Master
Client  <--ID_LOGIN_REQUEST_RETURN(status)------------  Master
        (only if status == SUCCESS)
Client  --ID_LOGIN(username, passwordHash, CRCs, HW)-->  Master
Client  <--ID_LOGIN_RETURN(status, playerId, ...)-----  Master
```

| Packet | ID | Dir | Purpose |
| --- | --- | --- | --- |
| `ID_LOGIN_REQUEST` | 108 | C→M | username + client version pre-check |
| `ID_LOGIN_REQUEST_RETURN` | 109 | M→C | accept / reject the request |
| `ID_LOGIN` | 110 | C→M | full credentials + machine fingerprint |
| `ID_LOGIN_RETURN` | 111 | M→C | result + account/session data |

### Round 1 — `Packet_ID_LOGIN_REQUEST` (built/sent by `FUN_0049d090`, rva `0x9d090`)

| Field | Type | |
| --- | --- | --- |
| `username` | `u8[64]` | |
| `clientVersion` | `u16` | build number; server expects **1853** (= 1.8.5.3) |

Server replies `ID_LOGIN_REQUEST_RETURN { status, username }` where status is
`LoginRequestReturnStatus`: `INVALID_INFO=0`, `SUCCESS=1`,
`OUTDATED_CLIENT=2`, `ALREADY_LOGGED_IN=3`.

### Round 2 — `Packet_ID_LOGIN` (built/sent by `HandlePacket_ID_LOGIN_REQUEST_RETURN`, rva `0x9ca70`)

On `SUCCESS`, the handler assembles `Packet_ID_LOGIN` (2760 bytes) and
`SendPacket(..., MASTER)` (see [[Packet Transport]]):

| Field | Off | Type | Source |
| --- | --- | --- | --- |
| `username` | `0x430` | `u8[64]` | login object `this+0x91` |
| `passwordHash` | `0x470` | `u8[64]` | computed hash (below) |
| `fileCRCs` | `0x4b0` | `vector<u32>` | **3 CRCs in order: fom_client.exe, cshell.dll, object.lto** (integrity check) |
| `macAddress` | `0x4c0` | `u8[32]` | primary adapter MAC |
| `driveModels` | `0x4e0` | `u8[64][4]` | up to 4 disk model strings |
| `driveSerialNumbers` | `0x5e0` | `u8[32][4]` | up to 4 disk serials |
| `loginToken` | `0x660` | `u8[64]` | launcher/Steam token, `this+0x111` *(role unverified)* |
| `computerName` | `0x6a0` | `u8[32]` | `GetComputerNameA` |
| `hasSteamTicket` | `0x6c0` | `bool` | Steam auth path |
| `steamTicketLength` | `0x6c4` | `u32` | |
| `steamTicket` | `0x6c8` | `u8[1024]` | Steam encrypted app ticket |

The `fileCRCs` triplet is the client-integrity check; a mismatch returns
`LOGIN_RETURN_INTEGRITY_CHECK_FAILED` (this is the "Game integrity check failed"
seen in `fom.log`). The MAC / drive / computer-name fields are a hardware
fingerprint (ban evasion / `DUPLICATE_IP`-style enforcement).

### Result — `Packet_ID_LOGIN_RETURN` (2944 bytes)

`status` is `LoginReturnStatus`: `INVALID_LOGIN=0`, `SUCCESS=1`,
`UNKNOWN_USERNAME=2`, `INCORRECT_PASSWORD=4`, `CREATE_CHARACTER=5`,
`TEMP_BANNED=7`, `PERM_BANNED=8`, `DUPLICATE_IP=9`,
`INTEGRITY_CHECK_FAILED=10`, `RUN_AS_ADMIN=11`, `ACCOUNT_LOCKED=12`,
`NOT_PURCHASED=13`. Also carries `playerId`, `accountType`, `isVolunteer`,
`isNewPlayer`, `clientVersion`, ban details, `factionMOTD`, `defaultApartment` +
`defaultApartmentWorldId`, and `loginWorldId` (the world to enter).

> The server's `LoginHandler` maps to a subset of these — `InvalidLogin`,
> `CreateCharacter`, `Success` — and a missing account returns `CreateCharacter`
> for a fresh login. (Server-side detail; see the server vault.)

## Password hashing — matches the server exactly

The hash is computed in `HandlePacket_ID_LOGIN_REQUEST_RETURN` using an inline
**MD5** implementation. MD5 is confirmed by the init constants in `FUN_00565200`
(`0x67452301, 0xefcdab89, 0x98badcfe, 0x10325476`); the helpers are
Init=`0x565200`, Update=`0x565240`, Final=`0x565320`, and digest→hex via
`0x565550`→`0x565460`, which uses the **uppercase** table
`s_0123456789ABCDEF`.

Algorithm:

```
inner   = MD5(password_bytes)            // password from login object this+0xd1
pwHex   = UPPER_HEX(inner)               // 32 chars
salted  = pwHex + username               // username appended as salt
outer   = MD5(salted_bytes)
passwordHash = UPPER_HEX(outer)          // 32 chars -> the wire field
```

This is **identical** to the server's stored scheme
`MD5( MD5(password)_hex_uppercase + username )`. The server does **not** verify
it yet (the `Check Password` TODO in `LoginHandler.cs`), but no client change is
needed when it does — and a row inserted with that hash (e.g. the test account
`tom`) is already wire-compatible.

> End-to-end password *rejection* is therefore untested (server accepts any
> password today), but the hashing algorithm itself is verified from the
> decompiled MD5 + uppercase-hex code.

## Reproduce

```bash
fomre type /FOM/Packets/Packet_ID_LOGIN
fomre type /FOM/Enums/Packets/LoginReturnStatus
fomre decompile fom_client.exe:0x0049ca70   # HandlePacket_ID_LOGIN_REQUEST_RETURN
fomre decompile fom_client.exe:0x00565200   # MD5_Init (magic constants)
fomre decompile fom_client.exe:0x00565460   # digest -> UPPERCASE hex
```
