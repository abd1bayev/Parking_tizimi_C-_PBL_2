#!/usr/bin/env sh

set -eu

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

exec ./src/ParkingTizimi.Desktop/bin/Debug/net8.0/ParkingTizimi.Desktop "$@"