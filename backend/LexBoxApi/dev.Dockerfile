# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
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
RUN dotnet build #build and restore, should speed up watch run
RUN mkdir /src/frontend
# no need to restore because we already restored as part of building the image
CMD dotnet watch run -lp docker --property:InformationalVersion=dockerDev --no-hot-reload --no-restore
