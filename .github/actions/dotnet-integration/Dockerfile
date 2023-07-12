FROM mcr.microsoft.com/dotnet/sdk:7.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends python2 \
    && rm -rf /var/lib/apt/lists/*

CMD ["sh", "-c", "dotnet test --logger trx --results-directory ./testresults --filter Category=Integration"]
