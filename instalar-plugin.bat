@echo off
setlocal EnableDelayedExpansion
rem Instala o plugin ActiveX (webrec.cab) do DVR Intelbras/Dahua sem Internet Explorer.
rem O plugin e baixado do PROPRIO DVR (http://IP-DO-DVR/webrec.cab) ou lido de plugin\webrec.cab.
rem Faz o mesmo que o webrec.inf: copia para C:\Program Files\webrec\VD3100 e registra os OCX em 32 bits.
rem Executar como administrador.

net session >nul 2>&1
if errorlevel 1 (
  echo Este instalador precisa ser executado como ADMINISTRADOR.
  echo Clique com o botao direito em instalar-plugin.bat e escolha "Executar como administrador".
  pause
  exit /b 1
)

set "CAB=%~dp0plugin\webrec.cab"
set "TRAB=%TEMP%\dvrviewer-plugin"
set "DESTINO=%SystemDrive%\Program Files\webrec\VD3100"
set "REGSVR=%WINDIR%\SysWOW64\regsvr32.exe"

if exist "%CAB%" (
  echo Usando o plugin local: "%CAB%"
) else (
  set "IP=%~1"
  if "!IP!"=="" set /p "IP=Endereco IP do DVR [192.168.1.108]: "
  if "!IP!"=="" set "IP=192.168.1.108"
  if not exist "%~dp0plugin" mkdir "%~dp0plugin"
  echo Baixando http://!IP!/webrec.cab ...
  curl.exe -f -s -o "%CAB%" "http://!IP!/webrec.cab"
  if errorlevel 1 (
    echo Nao foi possivel baixar o plugin do DVR !IP!.
    echo Confira o IP e se o DVR abre no navegador. Alguns modelos usam outro nome de arquivo.
    pause
    exit /b 1
  )
)

if exist "%TRAB%" rd /s /q "%TRAB%"
mkdir "%TRAB%"
expand.exe -F:* "%CAB%" "%TRAB%" >nul
if not exist "%TRAB%\*.ocx" (
  echo O arquivo "%CAB%" nao contem controles ActiveX.
  pause
  exit /b 1
)

echo Copiando arquivos para "%DESTINO%"...
if not exist "%DESTINO%" mkdir "%DESTINO%"
copy /y "%TRAB%\*.*" "%DESTINO%\" >nul || (echo Falha ao copiar. & pause & exit /b 1)

echo Registrando controles ActiveX...
set FALHAS=0
for %%F in ("%DESTINO%\*.ocx") do (
  "%REGSVR%" /s "%%~fF"
  if errorlevel 1 (
    echo   ERRO ao registrar %%~nxF
    set FALHAS=1
  ) else (
    echo   OK  %%~nxF
  )
)
rd /s /q "%TRAB%"

echo.
if "%FALHAS%"=="0" (
  echo Plugin instalado. Abra o DvrViewer.exe.
) else (
  echo Alguns controles nao registraram. Copie esta tela e abra uma issue no GitHub.
)
pause
