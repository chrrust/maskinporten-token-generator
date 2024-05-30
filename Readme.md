# MaskinportenTokenGetter

This is a simple tool to get a Maskinporten token or Altinn token. 

## Installation MacOS

```bash
dotnet publish -c Release -r osx-arm64 --self-contained --output ~/.token-getter
```

```bash
sudo ln -s ~/.token-getter/token-getter /usr/local/bin/token-getter
```

```bash
chmod +x /usr/local/bin/token-getter
```