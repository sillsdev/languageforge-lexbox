FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["LexBoxApi/LexBoxApi.csproj", "LexBoxApi/"]
RUN dotnet restore "LexBoxApi/LexBoxApi.csproj"
COPY . .
WORKDIR "/src/LexBoxApi"
RUN dotnet build "LexBoxApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LexBoxApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LexBoxApi.dll"]
