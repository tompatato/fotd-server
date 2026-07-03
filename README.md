# Face of Mankind Server Emulator

This project is a server emulator for the MMORPG "Face of Mankind". It aims to replicate
the functionality of the original game servers, allowing players to experience the game
using their own private servers.

## Getting Started

### Prerequisites

- [**.NET 10.0**](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- [**Docker**](https://docs.docker.com/engine/install/): In addition to a development server, Docker is necessary to build the project on Linux.
  This is because the included RakNet library requires GCC 4.8 to build, which is not available on
  modern operating systems.
- [**just**](https://github.com/casey/just): A command runner that simplifies working with tools.
- [**CMake 3.26 (or newer)**](https://cmake.org/download)
- [**ClangFormat**](https://clang.llvm.org/docs/ClangFormat.html): A C++ code formatter.

### Building

#### Windows

Visual Studio 2022 provides building for both C++ and C# components. Open the `FOMServer.sln` and then switch to [the "CMake Targets" view.](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170)
From there, "Configure" the FOMServer Project and it will generate the necessary CMake files for Visual Studio's integration. You will
now be able to build both "Master Server" and "World Server" from the solution view. If you enable
[mixed-mode debugging](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-in-mixed-mode?view=vs-2022&pivots=programming-language-cpp)
you will be able to step through both C++ and C# code using the Visual Studio debugger.

#### macOS / Linux

One of the caveats is that building the C++ components requires GCC 4.8, which is ancient and not supported on modern operating systems.
To address this, a Docker container is provided that handles building the project. `just docker-build` will build both the C++ and
C# components and publish the servers for you. `just publish` additionally drops a host-visible copy in `build/linux/{master,world}`.

The Docker build images are (re)built automatically, but you can build them explicitly with `just docker-images`.
Build output lands in `out/build/<config>/…`; the deployable artifact is published to `out/publish/{master,world}`.

### Testing

#### Windows

Visual Studio supports running the test suite using the Test Explorer.

#### macOS / Linux

```bash
# Run all tests.
just test

# Run them individually.
just test-cpp
just test-dotnet
```

### Development Server

```bash
# Bring up the whole stack: database, master server, and world server(s).
just server-up

# …or start pieces individually (each dependency is started automatically):
just db-up   # MariaDB (persistence)
just ms-up   # master server (login / world directory)
just ws-up   # world server(s)

# Tear everything back down.
just server-down
```

The database schema is created and kept up to date automatically: the **master
server runs the [FluentMigrator](https://fluentmigrator.github.io/) migrations on
startup** (see `server-shared/Infrastructure/Migrations`), so a fresh database
needs no manual setup.

Using Visual Studio you can also debug the servers directly as long as a database server is running.

### Connecting a client

The servers speak the original **Face of Mankind** client's protocol over
RakNet/UDP. Point a client at your master server by launching it with
`+MasterServer <host>` (the master listens on `61000/udp`; a world server *N*
listens on `61000 + N`). The client handshake, character creation, and world
handoff are documented in the knowledge base below.

## Documentation

- [`docs/architecture.md`](docs/architecture.md) — deep dive into packet flow, threading, routing, and persistence.
- [`docs/adding-packet-handlers.md`](docs/adding-packet-handlers.md) — step-by-step guide for adding a packet type end to end.
- [`docs/packet-identifiers.md`](docs/packet-identifiers.md) — the full packet-id space and what the emulator implements so far.
- [`knowledge-base/server`](knowledge-base/server) — server runtime/protocol notes (topology, ports, item & inventory lifecycle).
- [`knowledge-base/client`](knowledge-base/client) — reverse-engineering notes on the original client (login, inventory, GM commands, weapons/ammo, …).
