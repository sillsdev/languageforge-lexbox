# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
EXPOSE 80
EXPOSE 443
# Mercurial's bundled `hg` is a `#!/usr/bin/env python3` script, but this base (sdk:11.0, Ubuntu
# 26.04) ships python3 = 3.14 and Mercurial 6.5's demandimport breaks on 3.12+. Install python3.12
# from deadsnakes and alias it as python3 (nothing else in this dev image needs python), mirroring
# what the production Dockerfile does with a dedicated python3.12 stage.
RUN --mount=type=cache,target=/var/cache/apt,sharing=locked \
  --mount=type=cache,target=/var/lib/apt,sharing=locked \
  apt-get update && apt-get --no-install-recommends install -y tini iputils-ping ca-certificates curl gnupg \
  && curl -fsSL 'https://keyserver.ubuntu.com/pks/lookup?op=get&search=0xF23C5A6CF475977595C89F51BA6932366A755776' \
    | gpg --dearmor -o /etc/apt/keyrings/deadsnakes.gpg \
  && echo "deb [signed-by=/etc/apt/keyrings/deadsnakes.gpg] https://ppa.launchpadcontent.net/deadsnakes/ppa/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") main" \
    > /etc/apt/sources.list.d/deadsnakes.list \
  && apt-get update && apt-get --no-install-recommends install -y python3.12 \
  && ln -sf /usr/bin/python3.12 /usr/local/bin/python3
# Mercurial 6.5's demandimport breaks on 3.12+ (circular import of threading.RLock). Chorus spawns
# hg as a child, so this must be in the image env.
ENV HGDEMANDIMPORT=disable
RUN mkdir -p /var/lib/fw-headless /var/www/.local/share && chown -R www-data:www-data /var/lib/fw-headless /var/www/
USER www-data:www-data
WORKDIR /src/backend
# Uncomment line below if second COPY fails
# RUN mkdir -p FwLite && chown www-data:www-data FwLite
# Copy the main source project files
COPY --chown=www-data:www-data *.slnx FwHeadless/FwHeadless.csproj FixFwData/FixFwData.csproj LexCore/LexCore.csproj LexData/LexData.csproj Directory.Build.props Directory.Packages.props Harmony.props Harmony.*.References.props ./
# move them into the proper sub folders, based on the name of the project
RUN for file in $(ls *.csproj); do dir=${file%.*}; mkdir -p ${dir}/ && mv -v $file ${dir}/; done
# Do the same for csproj files in slightly different hierarchies
COPY --chown=www-data:www-data FwLite/FwDataMiniLcmBridge/FwDataMiniLcmBridge.csproj FwLite/LcmCrdt/LcmCrdt.csproj FwLite/MiniLcm/MiniLcm.csproj FwLite/FwLiteProjectSync/FwLiteProjectSync.csproj ./
RUN for file in $(ls *.csproj); do dir=${file%.*}; mkdir -p FwLite/${dir}/ && mv -v $file FwLite/${dir}/; done

ARG CACHE_LOCATION=/src/dotnet-cache
RUN --mount=type=cache,target=$CACHE_LOCATION,uid=33,gid=33 \
cp -r $CACHE_LOCATION/.local $CACHE_LOCATION/.nuget /var/www/ || true

# Now that all csproj files are in place, restore them
RUN dotnet restore FwHeadless/FwHeadless.csproj

#the cache needs to be stored in the image,
#so we can't use the cache on the restore command, so we back it up to the cache here

RUN --mount=type=cache,target=$CACHE_LOCATION,uid=33,gid=33  \
    cp -r /var/www/.local /var/www/.nuget $CACHE_LOCATION/

COPY --chown=www-data:www-data . .
WORKDIR /src/backend/FwHeadless
#build here so that the build is run before container start, need to make sure the property is set both here
#and in the CMD command, otherwise it will rebuild every time the container starts
RUN dotnet build --property:InformationalVersion=dockerDev

#ensures the shutdown happens quickly
ENTRYPOINT ["tini", "--"]

ENV ASPNETCORE_ENVIRONMENT=Development
# no need to restore because we already restored as part of building the image
CMD dotnet watch run --property:InformationalVersion=dockerDev --no-restore --non-interactive
