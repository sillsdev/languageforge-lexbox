#!/bin/bash

# Get the full path to the script directory
APPDIR="$(dirname "$(realpath "${BASH_SOURCE[0]}")")"

# Make it the current directory so rest of script will work
cd "$APPDIR" || {
        echo "Automatic install failed: couldn't load launcher template from $APPDIR"
        echo "You can create the FwLite launcher yourself by doing the following:"
        echo "  1. Copy $APPDIR/fwlite.desktop.template to your home folder"
        echo "  2. Edit it to replace the word APPDIR with ${APPDIR}"
        echo "  3. Rename it to fwlite.desktop and run chmod +x fwlite.desktop"
        echo "  4. Copy it into ${HOME}/.local/share/applications"
        exit 1
}

# Replace APPDIR in the template and create the actual .desktop file
sed "s|APPDIR|$APPDIR|g" fwlite.desktop.template > fwlite.desktop

# Make it executable
chmod +x fwlite.desktop

# Ensure the applications folder exists
mkdir -p ~/.local/share/applications

# Copy the desktop file to the launcher
cp fwlite.desktop ~/.local/share/applications/fwlite.desktop

echo "Installed FieldWorks Lite launcher!"

