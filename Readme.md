# MaskinportenTokenGetter

This is a simple tool to get a Maskinporten token or Altinn token. 

## Installation MacOS

_For ARM 64_
```bash
dotnet publish -c Release -r osx-arm64 --self-contained --output ~/.token-getter
```

_For x64_
```bash
dotnet publish -c Release -r osx-x64 --self-contained --output ~/.token-getter
```

Then create a symlink to the executable in the path

```bash
sudo ln -s ~/.token-getter/token-getter /usr/local/bin/token-getter
```

```bash
chmod +x /usr/local/bin/token-getter
```