# `fomre` — Reverse-engineering harness

Tooling that makes the FOM client's internals queryable from the command line:
it reads the committed static RE database under [`disassembly/`](../../disassembly)
and bridges it to the **live client process** running under Wine/Proton.

No Ghidra and no game binary are needed to use the static half — the symbol and
type JSON already lives in the repo. The live half needs the client running.

## Layout

```
tools/re/
  symdb.py     static symbol + type database over disassembly/*.json (stdlib only)
  memory.py    live process memory: find PID, module bases, read/write/scan via /proc
  ghidra.py    headless-Ghidra bridge: decompile / xref against the FOTD project
  fomre.py     CLI tying them together
  tests/       unittest suite over the committed JSON (no game/Ghidra, CI-safe)
disassembly/scripts/
  decompile.py one function -> decompiled C (PyGhidra postScript, JSON out)
  xref.py      references to/from an address or function (PyGhidra postScript)
```

## The three questions this answers

**1. What makes the binary accessible from the CLI?**
The hand-built Ghidra analysis is exported to diffable JSON (functions with
addresses/signatures/namespaces, typed globals, struct/enum layouts with field
offsets). `symdb.py` indexes it: resolve a name to an address, list a class's
members, print a struct's layout, map an enum value to its name.

**2. Does Ghidra have a CLI?**
Yes — `analyzeHeadless` and **PyGhidra** (`pyghidra_launcher.py --headless`).
This repo drives it via `just ghidra-gen` / `just ghidra-dump`
(see [`disassembly/README.md`](../../disassembly/README.md)) to rebuild/export the
labelled project headless. `ghidra.py` adds on-demand **decompile** and **xref**
on top: it runs `decompile.py` / `xref.py` as headless postScripts against the
built `disassembly/FOTD` project and parses their JSON. See *Ghidra setup* below.

**3. Application memory values?**
The Windows client runs as a normal Linux process under Wine/Proton, so its PE
modules are file-backed mappings in `/proc/<pid>/maps`. `memory.py` finds the
process, recovers each module's **load base** (modules get relocated — e.g.
CShell.dll loads at `0x763e0000`, not its preferred `0x10000000`), and
reads/scans `/proc/<pid>/mem`. A static symbol's live address is
`module_base + (addr - imageBase)`.

## Usage

```bash
# --- static (no game needed) ---
python3 tools/re/fomre.py programs                      # modules + image bases
python3 tools/re/fomre.py sym Player                     # search symbols
python3 tools/re/fomre.py sym FillUpdate --exact         # name -> addr / RVA
python3 tools/re/fomre.py type ItemDefinition            # struct field layout
python3 tools/re/fomre.py type ItemCategory              # enum members

# --- live (client running under Wine/Proton) ---
python3 tools/re/fomre.py pid                            # find client + module bases
python3 tools/re/fomre.py read CShell.dll:0x103c3fa8 --type ptr
python3 tools/re/fomre.py struct CShell.dll:0x1030dff0 /FOM/Types/Item/ItemDefinition
python3 tools/re/fomre.py scan u32 1000                  # find addresses holding 1000

# --- Ghidra (needs the FOTD project + a Ghidra 12.0.4 install; see below) ---
python3 tools/re/fomre.py decompile "FOM::Player::FillUpdate"     # -> decompiled C
python3 tools/re/fomre.py xref "FOM::Player::FillUpdate"          # callers
python3 tools/re/fomre.py xref "FOM::Player::SendUpdate" --direction from
```

## Ghidra setup (for `decompile` / `xref`)

These need a **Ghidra 12.0.4** install (the version the `disassembly/` export is
pinned to) and the labelled project built once with `just ghidra-gen`.

Ghidra 12.0.4 requires a **JDK in [21, 24]** and **PyGhidra** on **Python
3.10–3.13** (the bundled `jpype` wheels stop at 3.13). If the host's default JDK
or Python is newer, point Ghidra at compatible ones — `ghidra.py` resolves them:

- Ghidra: `$GHIDRA_INSTALL_DIR` → `tools/re/ghidra.local.json` `install_dir` → `/opt/ghidra`
- JDK: `$FOTD_GHIDRA_JDK` / `$JAVA_HOME` → `ghidra.local.json` `jdk`

`tools/re/ghidra.local.json` (gitignored) holds this machine's paths, e.g.:

```json
{ "install_dir": "/path/to/ghidra_12.0.4_PUBLIC", "jdk": "/path/to/jdk-21" }
```

PyGhidra installs into a Ghidra-managed venv on first `ghidra-gen`; if the
default `python3` is unsupported, create the venv from a 3.10–3.13 interpreter
and the launcher reuses it. Without any of this, `decompile`/`xref` raise a clear
`GhidraUnavailable` — the static DB and live-memory commands are unaffected.

A read/struct target is either a **symbol name** (resolved via the DB) or an
explicit `program:0xADDR` (the in-image address, as stored in the JSON) — use the
latter when a name exists in more than one module.

## Live-memory requirements

Reading another process's memory needs ptrace access:

- **same UID** as the client, **and**
- `kernel.yama.ptrace_scope = 0` (this host is already `0`), or run the harness
  as the client's parent.

On `EACCES`/`EPERM` the tool prints the exact reason and the
`sudo sysctl kernel.yama.ptrace_scope=0` remedy. Writes are **off by default**;
set `FOTD_RE_ALLOW_WRITE=1` to enable `write_mem`.

## Tests

```bash
just re-test          # or:
python3 -m unittest discover -s tools/re/tests
```

The suite validates symbol resolution, RVA math, image bases, struct/enum
layouts, and scalar decoding against the committed JSON — it needs neither
Ghidra nor a running game, so it runs in CI.
