## FieldWorks Lite Web runs as a terminal application with a web front-end in your browser

## Install a FieldWorks Lite shortcut into the Ubuntu launcher

1. Run install-launcher.sh
2. Open the Ubuntu launcher and search for "FieldWorks Lite"
3. You will see the FW Lite icon, click on that to run the application

## Run FieldWorks Lite manually
1. Open a terminal and run ./FwLiteWeb
2. On some systems you may be able to double-click the file

# App Configuration
1. On startup, a port will be chosen randomly, visible in the terminal output
2. Settings are configured in appsettings.json; you can configure the port and project folders there

# Closing the program
1. Close the web browser window or tab
2. Close the terminal window that is running FieldWorks Lite or Ctrl-C
3. If you never had a terminal window (e.g. double-clicked the FwLiteWeb file), you can kill the process in a terminal with `killall FwLiteWeb`