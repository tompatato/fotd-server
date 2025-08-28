#!/bin/bash
set -euo pipefail

# Ensure tests run on the already-built output
cd /out/cpp

# Pass any arguments directly to ctest
ctest "$@"
