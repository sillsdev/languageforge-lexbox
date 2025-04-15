### MacOS specific requirements
* Install ICU4c (MacOS does not have ICU installed by default)
  * `brew install icu4c`
  * Add the following lines to your ~/.zshrc (Apple Silicon)
  ```
  export DYLD_LIBRARY_PATH=/opt/homebrew/opt/icu4c/lib:$DYLD_LIBRARY_PATH
  export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
  ```
