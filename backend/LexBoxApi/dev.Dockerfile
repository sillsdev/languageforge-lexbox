# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
EXPOSE 80
EXPOSE 443
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
  --mount=type=cache,target=/var/lib/apt,sharing=locked \
  apt update && apt-get --no-install-recommends install -y tini
RUN mkdir -p /var/www && chown -R www-data:www-data /var/www
USER www-data:www-data
WORKDIR /src/backend
# Copy the main source project files
COPY */*.csproj *.sln Directory.Build.props ./
# move them into the proper sub folders, based on the name of the project
RUN for file in $(ls *.csproj); do dir=${file%.*} mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done; dotnet restore FixFwData/FixFwData.csproj; dotnet restore LexBoxApi/LexBoxApi.csproj

COPY --chown=www-data . .
WORKDIR /src/backend/LexBoxApi
#build here so that the build is run before container start, need to make sure the property is set both here
#and in the CMD command, otherwise it will rebuild every time the container starts
RUN dotnet build --property:InformationalVersion=dockerDev
RUN mkdir /src/frontend

#ensures the shutdown happens quickly
ENTRYPOINT ["tini", "--"]

# no need to restore because we already restored as part of building the image
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_URLS=http://0.0.0.0:5158
CMD dotnet watch --no-hot-reload run --property:InformationalVersion=dockerDev --no-restore
