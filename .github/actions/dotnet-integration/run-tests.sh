dotnet restore
dotnet build
pwsh backend/Testing/bin/Debug/net7.0/playwright.ps1 install --with-deps
dotnet test --logger trx --results-directory ./testresults --filter Category=Integration
