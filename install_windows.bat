@echo off
setlocal

echo Starting installation of token-getter...

mkdir "C:\Program Files\token-getter" 2>NUL

if "%PROCESSOR_ARCHITECTURE%"=="AMD64" (
    dotnet publish -c Release -r win-x64 --self-contained
    xcopy .\bin\Release\net8.0\win-x64 "C:\Program Files\token-getter" /E /I /H
) else (
    dotnet publish -c Release -r win-x86 --self-contained
    xcopy .\bin\Release\net8.0\win-x86 "C:\Program Files\token-getter" /E /I /H
)

REM Check if the path is already present
echo %PATH% | findstr /C:"C:\Program Files\token-getter" >nul
if %errorlevel%==0 (
    echo Path already contains the token-getter directory.
) else (
    setx /M PATH "%PATH%;C:\Program Files\token-getter"
    echo Added token-getter to the system PATH.
)

echo token-getter installed successfully.
endlocal
pause
