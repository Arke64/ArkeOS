CD %~dp0

SET Disk=%1
SET Assembler=.\ArkeOS.Compilation.Assembler\bin\Debug\ArkeOS.Tools.Assembler.exe
SET LocalStorage=%LOCALAPPDATA%\Packages\fc7d35ed-c3ad-4862-946c-a21d4fde227c_35t6z52snzrg2\LocalState

"%Assembler%" ".\Images\Boot.asm"
"%Assembler%" ".\Images\%Disk%.asm"

COPY ".\Images\Boot.bin" "%LocalStorage%\Boot.bin"
COPY ".\Images\%Disk%.bin" "%LocalStorage%\Disk 0.bin"