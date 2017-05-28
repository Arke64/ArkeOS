@ECHO OFF

SETLOCAL

SET ScriptRoot=%~dp0

PUSHD "%ScriptRoot%"

SET Disk=%1
SET PackageId=fc7d35ed-c3ad-4862-946c-a21d4fde227c
SET Assembler=%ScriptRoot%\ArkeOS.Tools.Assembler\bin\Debug\netcoreapp1.1\ArkeOS.Tools.Assembler.dll

FOR /D %%A IN ("%LOCALAPPDATA%\Packages\%PackageId%*") DO (
    SET PackageFolder=%%A
)

IF "%PackageFolder" == "" (
    ECHO Cannot find app pacakge folder.
    GOTO :EOF
)

IF "%Disk%" == ""  (
    ECHO Must provide a disk to run.
    GOTO :EOF
)

IF NOT EXIST "%Assembler%" (
    ECHO Cannot find assembler.
    GOTO :EOF
)

dotnet "%Assembler%" "%ScriptRoot%\Images\Boot.asm"
dotnet "%Assembler%" "%ScriptRoot%\Images\%Disk%.asm"

COPY /Y "%ScriptRoot%\Images\Boot.bin" "%PackageFolder%\LocalState\Boot.bin"
COPY /Y "%ScriptRoot%\Images\%Disk%.bin" "%PackageFolder%\LocalState\Disk 0.bin"

POPD

ENDLOCAL
