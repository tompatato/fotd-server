#!/bin/bash
set -euo pipefail

# Run tests, passing through any arguments
dotnet test /workspace --no-build "$@"
