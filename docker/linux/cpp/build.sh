#!/bin/bash
set -euo pipefail

# Sync source into container-local workspace
rsync -a --delete \
	--exclude='.vs' \
	--exclude='.vscode' \
	--exclude='.git' \
	--exclude='/out' \
	--exclude='/*-server/bin' \
	--exclude='/*-server/obj' \
	/src/ /workspace/

# Configure and build, passing through any extra args to CMake
mkdir -p /out/cpp
cd /workspace
cmake -S /workspace -B /out/cpp/build/debug "$@"
cmake --build /out/cpp/build/debug --config Debug -j$(nproc)
