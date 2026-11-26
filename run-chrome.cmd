@echo off
REM Builds and runs UserManagement.Web, then opens it in Google Chrome.
REM Double-click this file, or run "run-chrome" from a Command Prompt / PowerShell in this folder.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0run-chrome.ps1"
if errorlevel 1 pause
