# Keyboard Text Entry

How typed keystrokes become characters in the client's UI text fields (chat input,
dialogs). The client has **no** scancode→ASCII table of its own — it delegates
entirely to the Win32 keyboard-layout API, so under Wine the active keyboard
layout decides what works. This is why, in the chat box, letters/digits and the
digit-row shifted symbols type fine but the **OEM punctuation keys**
(`- = _ + / \ ; : ' @` …) produce nothing when *typed* — while the same
characters insert fine via emote menu / ↑ history / the game's internal paste.
All addresses are `CShell.dll` RVAs (image base `0x10000000`).

## The keystroke → character path

The high-level handlers you'd expect (`CWindowInputLine::OnEvent` rva `0xfb330`,
`CWindowEdit::OnEvent` rva `0xd0a20`) only do UI plumbing (autocomplete, confirm
dialogs) — **not** keystrokes. Text entry lives in the LithTech
`CLTGUITextCtrl` created by `CWindowInputLine::SetupControls` (rva `0xf9000`) via
`CBaseWindow::CreateTextCtrl` (rva `0x48950`); the control ctor is `FUN_10078d60`
(rva `0x78d60`, vftable `0x102c6854`).

| Step | Func (rva) | Role |
| --- | --- | --- |
| `HandleKeyDown` (vtable slot `0x18`) | `0x77b00` | receives a **Win32 Virtual-Key code** (cases: `8`=BACK, `9`=TAB, `0x10/0x11`=Shift/Ctrl, arrows `0x25–0x28`, `A/C/V/X` for Ctrl combos). Printable keys → `default:`. |
| insert gate | `0x76ed0` | AltGr/Ctrl guard (see below), then calls the translator and only inserts if it returns non-zero. |
| **translator** | **`0x75a50`** | VK → character via the Win32 layout API (below). |
| raw inserter | `0x75b50` | inserts a literal char. **Paste / emote / history call this directly**, bypassing the translator — which is why `/` works from those sources. |

### The translator — `FUN_10075a50` (rva `0x75a50`)

```c
HKL  hkl  = GetKeyboardLayout(0);
if (!GetKeyboardState(keyState)) return 0;        // Shift/Caps state
UINT scan = MapVirtualKeyExA(vk, 0 /*VK_TO_VSC*/, hkl);
if (scan == 0) return 0;                          // OEM keys die here under Wine
WORD out = 0;
ToAsciiEx(vk, scan, keyState, &out, 0, hkl);
return (char)out;                                 // 0 if untranslated
```

`user32.dll` imports: `GetKeyboardLayout`, `GetKeyboardState`, `MapVirtualKeyExA`,
`ToAsciiEx`. The `default:` branch guard in `FUN_10076ed0` ignores a key only when
**Ctrl is held without right-Alt** (`GetAsyncKeyState(VK_CONTROL 0x11)` set and
`GetAsyncKeyState(VK_RMENU 0xa5)` clear) — a bare OEM keypress passes it.

## Why the specific keys fail (root cause)

- Letters (`VK 0x41…`) and digits (`VK 0x30…`) map on every layout. `ToAsciiEx`
  reads the Shift bit from `GetKeyboardState`, so **Shift+digit** → `! " £ $ %
  ^ & * ( )`. This is why the number row (shifted or not) works.
- The OEM keys arrive as OEM virtual-keys (`VK_OEM_MINUS 0xBD`, `VK_OEM_PLUS 0xBB`,
  `VK_OEM_1 0xBA` `;:`, `VK_OEM_7 0xDE` `'@`, `VK_OEM_2 0xBF` `/?`,
  `VK_OEM_5 0xDC` `\|`, …). They reach `FUN_10075a50` but
  `MapVirtualKeyExA`/`ToAsciiEx` return **0** for them under the active Wine
  layout → no character inserted.

So it is **not** a client filter, not a missing client table, and not GM-gating —
it is Wine handing the client OEM VK codes its selected layout can't translate.
(This is the actual reason `/` couldn't be typed while chasing the `/give` /
[[Account Access Levels|GM command]] thread — unrelated to account level.)

## Root cause (confirmed) — XWayland reports `us`, keyboard is UK `gb`

**Confirmed on this host** (KDE Plasma on **Wayland**, game under XWayland): the
physical keyboard is UK (`gb`) but XWayland presents the layout as **`us`** to
Wine, so `GetKeyboardLayout(0)`/`ToAsciiEx` translate against a US layout the
running session isn't actually using — and the OEM keys that differ between the
maps (`/ \ ; ' @` …) return `0`. `setxkbmap -query` shows the mismatch. It is a
host/XWayland layout problem, not the Wine prefix registry and not the client.

## Fix — align the session keyboard layout (config-fixable)

1. **Confirmed fix — set the host layout to match the keyboard:** run
   `setxkbmap gb` in the KDE session, then relaunch the client (Wine reads the
   layout at startup). Make it permanent via KDE keyboard settings, or use a
   Plasma **X11** session instead of Wayland.
2. **Weaker fallback — the prefix layout.** In the prefix registry
   `HKEY_CURRENT_USER\Keyboard Layout\Preload`, set the KLID (`00000809` UK /
   `00000409` US — currently `00000409`). Edit only while the game is fully
   closed (Wine flushes the registry on exit and clobbers edits). Less reliable
   than the host layout under Wayland.
3. **Match the process locale** — e.g. `LANG=en_GB.UTF-8` so Wine picks the
   intended default layout.
4. **Try a newer Proton/Wine** — OEM VK/scancode mapping has improved across
   versions; an old build may fail where a newer one maps them.
5. **Do NOT** use a `ScancodeMap` remap — the failure is at the VK→char layer
   (`ToAsciiEx`), which `ScancodeMap` doesn't touch. Layout selection is the lever.

Runtime confirmation (optional): breakpoint/log `ToAsciiEx`'s return for an OEM
key under the current prefix — expect `0` before the fix.

## Reproduce

```bash
fomre decompile "CShell.dll:0x10075a50"   # translator: MapVirtualKeyExA + ToAsciiEx
fomre decompile "CShell.dll:0x10076ed0"   # insert gate + Ctrl/AltGr guard
fomre decompile "CShell.dll:0x10077b00"   # HandleKeyDown (receives Win32 VK codes)
fomre decompile "CShell.dll:0x100f9000"   # CWindowInputLine::SetupControls (creates the text ctrl)
```

See [[Client Architecture]] (engine/module split) and [[Account Access Levels]]
(the GM-command thread this input bug was masquerading as).
