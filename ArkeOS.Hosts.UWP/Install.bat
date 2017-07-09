@ECHO OFF

SETLOCAL

SET ScriptRoot=%~dp0

PUSHD "%ScriptRoot%"

SET BootDisk=%~n1
SET BootExtension=%~x1
SET AppDisk=%~n2
SET AppExtension=%~x2
SET PackageId=fc7d35ed-c3ad-4862-946c-a21d4fde227c
SET Assembler=%ScriptRoot%..\ArkeOS.Tools.Assembler\bin\Debug\netcoreapp1.1\ArkeOS.Tools.Assembler.dll
SET KohlCompiler=%ScriptRoot%..\ArkeOS.Tools.KohlCompiler\bin\Debug\netcoreapp1.1\ArkeOS.Tools.KohlCompiler.dll

FOR /D %%A IN ("%LOCALAPPDATA%\Packages\%PackageId%*") DO (
    SET PackageFolder=%%A
)

IF "%PackageFolder" == "" (
    ECHO Cannot find app pacakge folder.
    GOTO :EOF
)

IF "%AppDisk%" == ""  (
    ECHO Must provide a disk to run.
    GOTO :EOF
)

IF NOT EXIST "%Assembler%" (
    ECHO Cannot find assembler.
    GOTO :EOF
)

IF NOT EXIST "%KohlCompiler%" (
    ECHO Cannot find kohl compiler.
    GOTO :EOF
)

IF "%BootExtension%" == ".k" (
    dotnet "%KohlCompiler%" --output "%ScriptRoot%..\Images\%BootDisk%.bin" --src "%ScriptRoot%..\Images\%BootDisk%.k"
) ELSE (IF "%BootExtension%" == ".asm" (
    dotnet "%Assembler%" "%ScriptRoot%..\Images\%BootDisk%.asm"
))

IF "%AppExtension%" == ".k" (
    dotnet "%KohlCompiler%" --bootable --output "%ScriptRoot%..\Images\%AppDisk%.bin" --src "%ScriptRoot%..\Images\%AppDisk%.k"
) ELSE (IF "%AppExtension%" == ".asm" (
    dotnet "%Assembler%" "%ScriptRoot%..\Images\%AppDisk%.asm"
))

COPY /Y "%ScriptRoot%..\Images\%BootDisk%.bin" "%PackageFolder%\LocalState\Boot.bin"
COPY /Y "%ScriptRoot%..\Images\%AppDisk%.bin" "%PackageFolder%\LocalState\Disk 0.bin"

POPD

ENDLOCAL
