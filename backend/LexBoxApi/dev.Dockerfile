FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
EXPOSE 80
EXPOSE 443
RUN apt-get update && apt-get install -y python2
WORKDIR /src/backend/LexBoxApi
CMD dotnet watch -lp docker