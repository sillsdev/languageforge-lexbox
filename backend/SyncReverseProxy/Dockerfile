FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["SyncReverseProxy/SyncReverseProxy.csproj", "SyncReverseProxy/"]
RUN dotnet restore "SyncReverseProxy/SyncReverseProxy.csproj"
COPY . .
WORKDIR "/src/SyncReverseProxy"
RUN dotnet build "SyncReverseProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SyncReverseProxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SyncReverseProxy.dll"]
