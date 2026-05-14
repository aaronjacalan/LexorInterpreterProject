@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
set "INPUT_PATH=%~1"

if "%INPUT_PATH%"=="" (
  set "INPUT_PATH=%SCRIPT_DIR%TestingCode"
)

"%SCRIPT_DIR%bin\Debug\net9.0\LexorInterpreterProject.exe" "%INPUT_PATH%"
