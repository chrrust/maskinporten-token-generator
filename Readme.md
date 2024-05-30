# MaskinportenTokenGetter

This is a simple tool to get a Maskinporten token or Altinn token. 

## Installation MacOS

Use the install script for MacOS named `install_mac.sh` to install the tool. Below are some manual steps to install the tool if that is preferred.

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

## Installation Windows

Use the install script for windows named `install_windows.bat` to install the tool. Below are some manual steps to install the tool if that is preferred.

Terminal must be run as administrator for the following steps

```bash
mkdir "C:\Program Files\token-getter"
```

_For x64_
```bash
dotnet publish -c Release -r win-x64 --self-contained
xcopy .\bin\Release\net8.0\win-x64 "C:\Program Files\token-getter" /E /I /H
```

_For x86_
```bash
dotnet publish -c Release -r win-x86 --self-contained
xcopy .\bin\Release\net8.0\win-x86 "C:\Program Files\token-getter" /E /I /H
```

```bash
setx /M PATH "%PATH%;C:\Program Files\token-getter"
```

