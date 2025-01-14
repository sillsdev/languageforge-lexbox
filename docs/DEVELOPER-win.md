### Prerequisites
  * docker and compose
    * enable Kubernetes in the Docker Desktop settings

### Setup details
  * to install [Taskfile](https://taskfile.dev/installation/):
    * `winget install Task.Task` should do it
    * Or via npm: `npm install -g @go-task/cli`
  * to install [Tilt](https://docs.tilt.dev/) and add it to your path:
    * the Tilt installer will create a `bin` folder in your home folder and put the Tilt binary there
    * you may then need to add `C:\Users\YOUR_USER_NAME\bin` to your PATH
  * open PowerShell and run `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned`
    * this is necessary before running `task setup`, which uses a PowerShell script to download seed data
  * run `task setup` as per main instructions
  * add the following lines to your `C:\Windows\system32\drivers\etc\hosts` file:

```
127.0.0.1 resumable.localhost
127.0.0.1 hg.localhost
127.0.0.1 admin.localhost
```
