# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build

# Restore first, using only the project/props files, so this layer is cached and
# only re-runs when a .csproj or package version changes — not on every source
# edit. Keep this list in sync with LexBoxApi's ProjectReference graph; a missing
# entry makes the restore below fail loudly at build time.
COPY *.props ./
COPY LexBoxApi/LexBoxApi.csproj LexBoxApi/
COPY LexCore/LexCore.csproj LexCore/
COPY LexData/LexData.csproj LexData/
COPY LfClassicData/LfClassicData.csproj LfClassicData/
COPY SyncReverseProxy/SyncReverseProxy.csproj SyncReverseProxy/
COPY FwLite/MiniLcm/MiniLcm.csproj FwLite/MiniLcm/
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "LexBoxApi/LexBoxApi.csproj"

COPY . .

ARG APP_VERSION
LABEL version=$APP_VERSION

RUN --mount=type=cache,target=/root/.nuget/packages dotnet build /p:InformationalVersion=$APP_VERSION "/LexBoxApi/LexBoxApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish /p:InformationalVersion=$APP_VERSION "/LexBoxApi/LexBoxApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER www-data:www-data
ENTRYPOINT ["dotnet", "LexBoxApi.dll"]
