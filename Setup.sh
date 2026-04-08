#!/bin/bash

# Get the script directory and parent folder name
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"
cd "$SCRIPT_DIR"
PARENT_DIR_NAME="$(basename "$SCRIPT_DIR")"
UNITY_PROJECT_NAME="Unity${PARENT_DIR_NAME}"

# Display welcome message
echo "Welcome to HyperCasualTemplate setting up pipeline!!"
echo "This script will:"
echo "1. Change unity project name to $UNITY_PROJECT_NAME"
echo "2. Update submodules (GameFoundationCore, Extensions, Logging, UITemplate)"
echo "3. Update package name in ProjectSettings.asset"
read -p "Press enter to continue..."

# Remove old submodules
git rm HyperCasualTemplate/Assets/Submodules/GameFoundationCore/
git rm HyperCasualTemplate/Assets/Submodules/Extensions/
git rm HyperCasualTemplate/Assets/Submodules/Logging/
git rm HyperCasualTemplate/Assets/Submodules/UITemplate/

# Rename Unity Project
mv HyperCasualTemplate "$UNITY_PROJECT_NAME"

# Add template remote
git remote add template git@github.com:tranvietanh0/HyperCasualGameTemplate.git

# Re-add submodules
git submodule add git@github.com:tranvietanh0/GameFoundationCore.git "$UNITY_PROJECT_NAME/Assets/Submodules/GameFoundationCore"
git submodule add git@github.com:tranvietanh0/Unity.Extensions.git "$UNITY_PROJECT_NAME/Assets/Submodules/Extensions"
git submodule add git@github.com:tranvietanh0/Unity.Logging.git "$UNITY_PROJECT_NAME/Assets/Submodules/Logging"
git submodule add git@github.com:tranvietanh0/UITemplate.git "$UNITY_PROJECT_NAME/Assets/Submodules/UITemplate"
git submodule foreach git switch master

# Update product name and package name in ProjectSettings.asset
SETTINGS_FILE="$UNITY_PROJECT_NAME/ProjectSettings/ProjectSettings.asset"
if [ -f "$SETTINGS_FILE" ]; then
    sed -i "s/productName: HyperCasualTemplate/productName: $UNITY_PROJECT_NAME/" "$SETTINGS_FILE"
    sed -i "s/com\.DefaultCompany\.3D-Project/com.DefaultCompany.$UNITY_PROJECT_NAME/" "$SETTINGS_FILE"
fi

read -p "Press enter to continue..."
