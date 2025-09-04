#!/bin/bash
set -euo pipefail

# Usage: ./dev.sh [build|test] [cpp|dotnet|all] [extra args...]

if [[ $# -lt 3 ]]; then
	echo "Usage: $0 [build|test] [cpp|dotnet|all] [Debug/Release] [extra args...]"
	exit 1
fi

COMMAND=$1
TARGET=$2
CONFIG=$3
shift 3

# Validate command
if [[ "$COMMAND" != "build" && "$COMMAND" != "test" ]]; then
	echo "Error: invalid command '$COMMAND'. Must be 'build' or 'test'."
	exit 1
fi

# Validate target
if [[ "$TARGET" != "cpp" && "$TARGET" != "dotnet" && "$TARGET" != "all" ]]; then
	echo "Error: invalid target '$TARGET'. Must be 'cpp', 'dotnet', or 'all'."
	exit 1
fi

# Validate config
if [[ "$CONFIG" != "Debug" && "$CONFIG" != "Release" ]]; then
	echo "Error: invalid config '$CONFIG'. Must be 'Debug' or 'Release'."
	exit 1
fi

# Figure out the directory of this script
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
COMPOSE_FILE="$SCRIPT_DIR/docker-compose.yml"

run_docker() {
	docker compose -f "$COMPOSE_FILE" run --rm "$@"
}

case "$COMMAND" in
	build)
		case "$TARGET" in
		cpp)    run_docker cpp-build build.sh "$CONFIG" "$@" ;;
		dotnet) run_docker dotnet-build build.sh "$CONFIG" "$@" ;;
		all)
			run_docker cpp-build build.sh "$CONFIG" "$@"
			run_docker dotnet-build build.sh "$CONFIG" "$@"
			;;
		esac
		;;
	test)
		case "$TARGET" in
		cpp)
			[[ "${DEV_SKIP_BUILD:-0}" != "1" ]] && run_docker cpp-build build.sh "$CONFIG"
			run_docker cpp-build test.sh "$CONFIG" "$@"
			;;
		dotnet)
			[[ "${DEV_SKIP_BUILD:-0}" != "1" ]] && run_docker dotnet-build build.sh "$CONFIG"
			run_docker dotnet-build test.sh "$CONFIG" "$@"
			;;
		all)
			[[ "${DEV_SKIP_BUILD:-0}" != "1" ]] && run_docker cpp-build build.sh "$CONFIG"
			[[ "${DEV_SKIP_BUILD:-0}" != "1" ]] && run_docker dotnet-build build.sh "$CONFIG"
			run_docker cpp-build test.sh "$CONFIG" "$@"
			run_docker dotnet-build test.sh "$CONFIG" "$@"
			;;
	esac
esac
