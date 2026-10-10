@echo off
setlocal
echo ================================================================================
echo       EaseMEP (Smart MEP Router) — 1-Click Installer for Autodesk Revit
echo ================================================================================
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\deploy.ps1" -RevitVersion all
pause
