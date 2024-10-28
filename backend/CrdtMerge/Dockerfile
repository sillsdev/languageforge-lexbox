# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "CrdtMerge/CrdtMerge.csproj"

ARG APP_VERSION
LABEL version=$APP_VERSION

RUN --mount=type=cache,target=/root/.nuget/packages dotnet build /p:InformationalVersion=$APP_VERSION "CrdtMerge/CrdtMerge.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish /p:InformationalVersion=$APP_VERSION "CrdtMerge/CrdtMerge.csproj" -c Release -o /app/publish

FROM base AS final
RUN mkdir -p /var/lib/crdtmerge && chown -R www-data:www-data /var/lib/crdtmerge
WORKDIR /app
COPY --from=publish /app/publish .
USER www-data:www-data
ENTRYPOINT ["dotnet", "CrdtMerge.dll"]
