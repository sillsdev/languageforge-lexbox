# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
EXPOSE 80
EXPOSE 443
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
  --mount=type=cache,target=/var/lib/apt,sharing=locked \
  apt update && apt-get --no-install-recommends install -y rsync ssh
WORKDIR /src/backend
# Copy the main source project files
COPY */*.csproj *.sln ./
# move them into the proper sub folders, based on the name of the project
RUN for file in $(ls *.csproj); do dir=${file%.*} mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done; dotnet restore FixFwData/FixFwData.csproj; dotnet restore LexBoxApi/LexBoxApi.csproj

COPY . .
WORKDIR /src/backend/LexBoxApi
RUN mkdir /src/frontend
ENV DockerDev=true
CMD dotnet watch run -lp docker --property:InformationalVersion=dockerDev
