FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
# Copy the main source project files
COPY backend/*/*.csproj ./
# move them into the proper sub folders, based on the name of the project
RUN for file in $(ls *.csproj); do mkdir -p backend/${file%.*}/ && mv $file backend/${file%.*}/; done
RUN dotnet restore "backend/LexBoxApi/LexBoxApi.csproj"
COPY . .
WORKDIR "/src/backend/LexBoxApi"
RUN dotnet build "LexBoxApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LexBoxApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LexBoxApi.dll"]
