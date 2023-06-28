FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
EXPOSE 80
EXPOSE 443
WORKDIR /src/backend/LexBoxApi
ENV DockerDev=true
CMD dotnet watch run -lp docker --property:InformationalVersion=dockerDev
