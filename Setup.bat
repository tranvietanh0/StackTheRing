@echo off
cd /d %~dp0
for %%a in ("%~dp0\.") do set "parent=%%~nxa"
set UnityProjectName=Unity%parent%

title HyperCasualTemplate Setup Script
echo Welcome to HyperCasualTemplate setting up pipeline!!
echo This script will:
echo 1. Change unity project name to %UnityProjectName%
echo 2. Update submodules (GameFoundationCore, Extensions, Logging, UITemplate)
echo 3. Update package name in ProjectSettings.asset
pause

setlocal

@REM Remove old submodules
git rm HyperCasualTemplate\Assets\Submodules\GameFoundationCore\
git rm HyperCasualTemplate\Assets\Submodules\Extensions\
git rm HyperCasualTemplate\Assets\Submodules\Logging\
git rm HyperCasualTemplate\Assets\Submodules\UITemplate\

@REM Rename Unity Project
ren HyperCasualTemplate %UnityProjectName%

@REM Add template remote
git remote add template git@github.com:tranvietanh0/HyperCasualGameTemplate.git

@REM Re-add submodules
git submodule add git@github.com:tranvietanh0/GameFoundationCore.git %UnityProjectName%/Assets/Submodules/GameFoundationCore
git submodule add git@github.com:tranvietanh0/Unity.Extensions.git %UnityProjectName%/Assets/Submodules/Extensions
git submodule add git@github.com:tranvietanh0/Unity.Logging.git %UnityProjectName%/Assets/Submodules/Logging
git submodule add git@github.com:tranvietanh0/UITemplate.git %UnityProjectName%/Assets/Submodules/UITemplate
git submodule foreach git switch master

@REM Update product name and package name in ProjectSettings.asset
powershell -ExecutionPolicy Bypass -Command ^
  "$file = '%UnityProjectName%\ProjectSettings\ProjectSettings.asset'; " ^
  "$content = Get-Content $file -Raw; " ^
  "$content = $content -replace 'productName: HyperCasualTemplate', ('productName: ' + '%UnityProjectName%'); " ^
  "$content = $content -replace 'com\.DefaultCompany\.3D-Project', ('com.DefaultCompany.' + '%UnityProjectName%'); " ^
  "Set-Content $file $content -NoNewline"

pause
