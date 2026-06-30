# PositionRotation

The client's compact spatial encoding, used in [[WorldUpdate]] (`+0x08`).

## `FOM::Types::PositionRotation` (16 bytes)

| Offset | Field | Type |
| --- | --- | --- |
| `0x00` | `pos` | `Position` (below) |
| `0x0c` | `rot` | `u16` (packed yaw) |

## `FOM::Types::Position` (12 bytes)

| Offset | Field | Type |
| --- | --- | --- |
| `0x00` | `precision` | `u32` |
| `0x04` | `x` | `i16` |
| `0x06` | `y` | `i16` |
| `0x08` | `z` | `i16` |

Coordinates are **quantized to signed 16-bit** rather than sent as floats: the
engine's float position is scaled and rounded into `x/y/z` (see the position
handling in [[Player Update Flow]]), with `precision` carrying the scale/grid
context. Yaw is likewise packed into the single `rot` word. This keeps each
position to 12 bytes on the wire at the cost of spatial resolution.

Reproduce: `fomre type /FOM/Types/PositionRotation` and `… /FOM/Types/Position`.
