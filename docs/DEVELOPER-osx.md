### Prerequisites
  * [Docker Desktop for Mac](https://docs.docker.com/desktop/setup/install/mac-install/)
    * enable Kubernetes in the Docker Desktop settings

### Setup details
  * to install [Taskfile](https://taskfile.dev/installation/) and [Tilt](https://docs.tilt.dev/), for example with Homebrew:
    * `brew install go-task tilt`
    * check with `task --version` and `tilt version`
  * install ICU4C (macOS does not have ICU installed by default):
    * `brew install icu4c`
    * add the following lines to your ~/.zshrc (Apple Silicon):
  ```
  export DYLD_LIBRARY_PATH=/opt/homebrew/opt/icu4c/lib:$DYLD_LIBRARY_PATH
  export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
  ```
  * run `task setup` and continue with the [main instructions](../README.md)
