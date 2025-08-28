#!/bin/bash
set -euo pipefail

# Usage: ./dev.sh [build|test] [cpp|dotnet|all] [extra args...]
COMMAND=${1:-}
TARGET=${2:-}
shift 2 || true  # shift away command and target

case "$COMMAND" in
  build)
	case "$TARGET" in
	  cpp)
		docker compose -f docker/linux/docker-compose.yml run --rm cpp-build build.sh "$@"
		;;
	  dotnet)
		docker compose -f docker/linux/docker-compose.yml run --rm dotnet-build build.sh "$@"
		;;
	  all)
		docker compose -f docker/linux/docker-compose.yml run --rm cpp-build build.sh
		docker compose -f docker/linux/docker-compose.yml run --rm dotnet-build build.sh "$@"
		;;
	  *)
		echo "Unknown build target: $TARGET"
		exit 1
		;;
	esac
	;;
  test)
	case "$TARGET" in
	  cpp)
		docker compose -f docker/linux/docker-compose.yml run --rm cpp-build test.sh "$@"
		;;
	  dotnet)
		docker compose -f docker/linux/docker-compose.yml run --rm dotnet-build test.sh "$@"
		;;
	  all)
		docker compose -f docker/linux/docker-compose.yml run --rm cpp-build test.sh "$@"
		docker compose -f docker/linux/docker-compose.yml run --rm dotnet-build test.sh "$@"
		;;
	  *)
		echo "Unknown test target: $TARGET"
		exit 1
		;;
	esac
	;;
  *)
	echo "Usage: $0 [build|test] [cpp|dotnet|all] [extra args...]"
	exit 1
	;;
esac
