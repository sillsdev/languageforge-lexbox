#!/bin/bash

# Define the list of allowed commands
allowed_commands=("verify" "tip" "lexentrycount" "recover")

# Get the project code and command name from the URL
IFS='/' read -ra PATH_SEGMENTS <<< "$PATH_INFO"
project_code="${PATH_SEGMENTS[1]}"
command_name="${PATH_SEGMENTS[2]}"

# Ensure the project code and command name are safe to use in a shell command
if [[ ! $project_code =~ ^[a-z0-9][a-z0-9-]*$ ]] || [[ ! $command_name =~ ^[a-zA-Z0-9]+$ ]]; then
    echo "Content-type: text/plain"
    echo "Status: 400 Bad Request"
    echo ""
    echo "Invalid project code or command name."
    echo "Project code: $project_code"
    echo "Command name: $command_name"
    exit 1
fi

# Check if the command is in the list of allowed commands
if [[ ! " ${allowed_commands[@]} " =~ " ${command_name} " ]]; then
    echo "Content-type: text/plain"
    echo "Status: 400 Bad Request"
    echo ""
    echo "Invalid command. Allowed commands are: ${allowed_commands[*]}"
    echo "Command name: $command_name"
    exit 1
fi

# Start outputting the result right away so the HTTP connection won't be timed out
echo "Content-type: text/plain"
echo ""

# Run the hg command, simply output to stdout
cd /var/hg/repos/$project_code
case $command_name in

    lexentrycount)
        # The \b for word boundary is necessary to distinguish LexEntry from LexEntryType and similar
        chg cat -r tip Linguistics/Lexicon/Lexicon_{01,02,03,04,05,06,07,08,09,10}.lexdb | grep -c '<LexEntry\b'
        ;;

    tip)
        chg tip --template '{node}'
        ;;

    verify)
        # Env var PYTHONUNBUFFERED required for commands like verify and recover, so that output can stream back to the project page
        export PYTHONUNBUFFERED=1
        # Need a timeout so hg verify won't take forever on the "checking files" step
        timeout 5 chg verify
        ;;

    *)
        # Env var PYTHONUNBUFFERED required for commands like verify and recover, so that output can stream back to the project page
        PYTHONUNBUFFERED=1 chg $command_name
        ;;
esac
