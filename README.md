# Face of Mankind Server Emulator

This project is a server emulator for the MMORPG "Face of Mankind". It aims to replicate
the functionality of the original game servers, allowing players to experience the game
using their own private servers.

## Development

This project includes support for building and testing on both Windows and Linux.

### Windows

#### C++

Building and testing is done through CMake. On Windows, MSVC supports the same laxness
in C++ standards as RakNet. This allows it to target the older standard and build without
needing to resort to using an older compiler.

#### C#

A Visual Studio solution is provided for the C# components of the project. Once you've
opened the CMake Targets View it will generate the config files necessary to build the
entire project using the Solution view.

## Linux

#### C++

Due to the age of the included RakNet library, there are a few hurdles to jump. The
primary difficulty is that it was written for C++03 and relies on some lax behaviors
that are no longer allowed by modern compilers; even if the build is targeting the
older standard. The last version of GCC that is able to build RakNet is GCC 4.8.
To address this, a Docker environment is provided that is able to build the project.

Use the included [PowerShell](docker/linux/dev.ps1) or [bash](docker/linux/dev.sh)
scripts to simplify building and testing using the environment.

#### C#

For the sake of simplicity, the C# components are also able to be built and tested
using the included Docker environment.
