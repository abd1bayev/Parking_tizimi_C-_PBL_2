#!/usr/bin/env sh
export PATH="$HOME/.dotnet:$PATH"
cd "$(dirname "$0")"

echo ">> Build qilinmoqda..."
dotnet build src/Console/Console.csproj -v q --nologo || exit 1

echo ">> Console ishga tushmoqda..."
exec dotnet run --project src/Console/Console.csproj --no-build "$@"
