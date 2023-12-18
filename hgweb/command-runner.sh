#!/bin/bash

# Define the list of allowed commands
allowed_commands=("verify")

# Get the project code and command name from the URL
IFS='/' read -ra PATH_INFO <<< "$PATH_INFO"
project_code="${PATH_INFO[1]}"
command_name="${PATH_INFO[2]}"

# Ensure the project code and command name are safe to use in a shell command
if [[ ! $project_code =~ ^[a-z0-9-]+$ ]] || [[ ! $command_name =~ ^[a-zA-Z0-9]+$ ]]; then
    echo "Content-type: text/plain"
    echo ""
    echo "Invalid project code or command name."
    echo "Project code: $project_code"
    echo "Command name: $command_name"
    exit 1
fi

# Check if the command is in the list of allowed commands
if [[ ! " ${allowed_commands[@]} " =~ " ${command_name} " ]]; then
    echo "Content-type: text/plain"
    echo ""
    echo "Invalid command. Allowed commands are: ${allowed_commands[*]}"
    echo "Command name: $command_name"
    exit 1
fi

# Run the hg command
cd /var/hg/repos/$project_code
command_output=$(hg $command_name)

# Output the result
echo "Content-type: text/plain"
echo ""
echo "$command_output"
