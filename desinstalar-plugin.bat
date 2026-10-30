@echo off
rem Remove o plugin ActiveX instalado por instalar-plugin.bat. Executar como administrador.

net session >nul 2>&1
if errorlevel 1 (
  echo Execute como ADMINISTRADOR.
  pause
  exit /b 1
)

set "DESTINO=%SystemDrive%\Program Files\webrec\VD3100"
set "REGSVR=%WINDIR%\SysWOW64\regsvr32.exe"

for %%F in ("%DESTINO%\*.ocx") do "%REGSVR%" /s /u "%%~fF"
rd /s /q "%SystemDrive%\Program Files\webrec"
echo Plugin removido.
pause
