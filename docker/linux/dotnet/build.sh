#!/bin/bash
set -euo pipefail

CONFIG=${1:-Debug}

# Sync source into container-local workspace
rsync -a --delete \
	--exclude='.vs' \
	--exclude='.vscode' \
	--exclude='.git' \
	--exclude='/out' \
	--exclude='/*-server/bin' \
	--exclude='/*-server/obj' \
	/src/ /workspace/

# Build projects into /out/dotnet, forwarding args (e.g. -c Release)
mkdir -p /out/dotnet
cd /workspace
dotnet build master-server -c Debug -o /out/dotnet/master-server/Debug "$@"
dotnet build world-server -c Debug -o /out/dotnet/world-server/Debug "$@"
