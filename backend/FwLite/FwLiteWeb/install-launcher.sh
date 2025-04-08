#!/bin/bash

# Get the full path to the current directory
APPDIR="$(pwd)"

# Replace APPDIR in the template and create the actual .desktop file
sed "s|APPDIR|$APPDIR|g" fwlite.desktop.template > fwlite.desktop

# Make it executable
chmod +x fwlite.desktop

# Ensure the applications folder exists
mkdir -p ~/.local/share/applications

# Copy the desktop file to the launcher
cp fwlite.desktop ~/.local/share/applications/fwlite.desktop

echo "Installed FieldWorks Lite launcher!"

