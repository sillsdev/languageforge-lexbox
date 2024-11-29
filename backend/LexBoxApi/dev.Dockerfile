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
RUN for file in $(ls *.csproj); do dir=${file%.*}; mkdir -p ${dir}/ && mv -v $file ${dir}/; done
# Do the same for csproj files in slightly different hierarchies
COPY harmony/src/*/*.csproj ./
RUN for file in $(ls *.csproj); do dir=${file%.*}; mkdir -p harmony/src/${dir}/ && mv -v $file harmony/src/${dir}/; done
COPY harmony/src/Directory.Build.props harmony/Directory.Packages.props ./harmony/src/
COPY FwLite/*/*.csproj ./
RUN for file in $(ls *.csproj); do dir=${file%.*}; mkdir -p FwLite/${dir}/ && mv -v $file FwLite/${dir}/; done

ARG CACHE_LOCATION=/src/dotnet-cache
RUN --mount=type=cache,target=$CACHE_LOCATION,uid=33,gid=33 \
cp -r $CACHE_LOCATION/.local $CACHE_LOCATION/.nuget /var/www/ || true

# Now that all csproj files are in place, restore them
RUN dotnet restore FixFwData/FixFwData.csproj; dotnet restore LexBoxApi/LexBoxApi.csproj

#the cache needs to be stored in the image,
#so we can't use the cache on the restore command, so we back it up to the cache here

RUN --mount=type=cache,target=$CACHE_LOCATION,uid=33,gid=33  \
    cp -r /var/www/.local /var/www/.nuget $CACHE_LOCATION/

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
CMD dotnet watch run --property:InformationalVersion=dockerDev --no-restore --non-interactive
