#!/bin/bash

echo "Starting installation of token-getter..."

ARCH=$(uname -m)
OUTPUT_DIR="$HOME/.token-getter"

if [ "$ARCH" == "arm64" ]; then
    dotnet publish -c Release -r osx-arm64 --self-contained --output "$OUTPUT_DIR"
elif [ "$ARCH" == "x86_64" ]; then
    dotnet publish -c Release -r osx-x64 --self-contained --output "$OUTPUT_DIR"
else
    echo "Unsupported architecture: $ARCH"
    exit 1
fi

# Remove existing symlink if it exists
if [ -L /usr/local/bin/token-getter ]; then
    sudo rm /usr/local/bin/token-getter
fi

sudo ln -s "$OUTPUT_DIR/token-getter" /usr/local/bin/token-getter
sudo chmod +x /usr/local/bin/token-getter

echo "token-getter installed successfully."
