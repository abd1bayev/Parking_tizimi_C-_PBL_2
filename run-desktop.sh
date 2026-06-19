#!/usr/bin/env sh
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
cd "$(dirname "$0")"

echo ">> Build qilinmoqda..."
dotnet build src/Desktop/Desktop.csproj -v q --nologo || exit 1

echo ">> Desktop ochilmoqda..."
echo "   (Oyna ochilmasa, quyidagi loglarni yuboring)"
dotnet exec src/Desktop/bin/Debug/net8.0/Desktop.dll "$@"
