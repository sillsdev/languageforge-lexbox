### Prerequisites
  * docker and compose
    * enable Kubernetes in the Docker Desktop settings

### Setup
  * install [Taskfile](https://taskfile.dev/installation/)
    * `winget install Task.Task` should do it
    * Or via npm: `npm install -g @go-task/cli`
  * install [Tilt](https://docs.tilt.dev/) and add it to your path (don't forget to read the script before running it)
    * the Tilt installer will create a `bin` folder in your home folder and put the Tilt binary there
    * you may then need to add `C:\Users\YOUR_USER_NAME\bin` to your PATH
  * run `tilt version` to check that Tilt is installed correctly
  * clone the repo
  * run `git push` to make sure your GitHub credentials are set up
    * allow the Git Credential Manager to log in to GitHub via your browser
  * open PowerShell and run `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned`
    * this is necessary before running `task setup` below, which uses a PowerShell script to download seed data
  * run `task setup`, which:
    * initializes a local.env file
    * tells Git to use our ignore revs file
    * checks out Git submodules
    * downloads the FLEx repo for the project seed data
  * add the following lines to your `C:\Windows\system32\drivers\etc\hosts` file:

```
127.0.0.1 resumable.localhost
127.0.0.1 hg.localhost
127.0.0.1 admin.localhost
```
