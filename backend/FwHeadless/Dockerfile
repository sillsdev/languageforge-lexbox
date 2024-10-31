# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

COPY . .
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "FwHeadless/FwHeadless.csproj"

ARG APP_VERSION
LABEL version=$APP_VERSION

RUN --mount=type=cache,target=/root/.nuget/packages dotnet build /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/build

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages dotnet publish /p:InformationalVersion=$APP_VERSION "FwHeadless/FwHeadless.csproj" -c Release -o /app/publish

FROM base AS final
RUN mkdir -p /var/lib/fw-headless /var/www/.local/share && chown -R www-data:www-data /var/lib/fw-headless /var/www/.local/share
WORKDIR /app
COPY --from=publish /app/publish .
USER www-data:www-data
ENV XDG_DATA_HOME=/var/www/.local/share
ENTRYPOINT ["dotnet", "FwHeadless.dll"]
