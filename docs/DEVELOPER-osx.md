### Prerequisites
  * docker and compose
    * enable Kubernetes in the Docker Desktop settings

### Setup
  * install [Taskfile](https://taskfile.dev/installation/)
    * `brew install go-task/tap/go-task` should do it
    * Or via npm: `npm install -g @go-task/cli`
  * install [Tilt](https://docs.tilt.dev/) and add it to your path (don't forget to read the script before running it)
  * run `tilt version` to check that Tilt is installed correctly
  * clone the repo
  * run `git push` to make sure your GitHub credentials are set up
    * allow the Git Credential Manager to log in to GitHub via your browser
    * or upload your SSH key to your GitHub account if you haven't done so already, then run `git remote set-url --push origin git@github.com:sillsdev/languageforge-lexbox`
  * run `task setup`, which:
    * initializes a local.env file
    * tells Git to use our ignore revs file
    * checks out Git submodules
    * downloads the FLEx repo for the project seed data
