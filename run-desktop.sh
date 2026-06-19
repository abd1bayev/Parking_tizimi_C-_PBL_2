#!/usr/bin/env sh

set -eu

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"

exec dotnet run --project src/ParkingTizimi.Desktop "$@"