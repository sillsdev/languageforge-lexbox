### Prerequisites
  * [Docker Desktop for Linux](https://docs.docker.com/desktop/setup/install/linux/) (provides docker and compose)
    * using native docker can lead to permissions issues with shared volumes, and some of our code (e.g. the Tiltfile) assumes you're running Docker Desktop
    * enable Kubernetes in the Docker Desktop settings

### Setup details
  * to install [Taskfile](https://taskfile.dev/installation/):
    * `sudo snap install task --classic` or other options on their website
    * Or via npm: `npm install -g @go-task/cli`
  * to install [Tilt](https://docs.tilt.dev/) (don't forget to read the script before running it):
    * the script will install tilt into `$HOME/.local/bin`, creating it if it doesn't exist
      * most Linux distributions put `$HOME/.local/bin` in your PATH automatically
      * if `tilt version` doesn't work, try running `source $HOME/.bashrc`, or log out and log back in
  * optional but recommended: turn off logging for Docker Desktop
    * Docker Desktop's default is to log verbose messages to stderr, which by default gets sent to the systemd journal and/or /var/log/syslog
    * This can result in your /var partition filling up fast (one developer saw his syslog grow by multiple gigabytes in a single week)
    * To silence Docker Desktop logs, create the following drop-in at `$HOME/.config/systemd/user/docker-desktop.service.d/stop-log-spam.conf`:
    ```
    [Service]
    StandardError=null
    ```
    * This file can be named anything as long as it ends in `.conf` and is in the correct directory, e.g. `override.conf` would work. The directory must be named exactly as shown.
