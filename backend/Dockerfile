# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY . .
# WORKDIR /src
# Copy the main source project files
# COPY */*.csproj ./
#
# move them into the proper sub folders, based on the name of the project
# RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN --mount=type=cache,target=/root/.nuget/packages dotnet restore "LexBoxApi/LexBoxApi.csproj"
# COPY . .
# WORKDIR "/LexBoxApi"

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
