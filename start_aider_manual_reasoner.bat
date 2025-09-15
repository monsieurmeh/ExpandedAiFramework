@echo off
setlocal enabledelayedexpansion

:: Get API key
for /f "delims=" %%x in (IgnoreMe/tld-aider.txt) do set "API_KEY=%%x"
if not defined API_KEY (
    echo [ERROR] No API key found in IgnoreMe/tld-aider.txt
    exit /b 1
)

echo [AIDER] Starting in: %CD%

aider --model=deepseek/deepseek-reasoner --api-key deepseek=!API_KEY!
