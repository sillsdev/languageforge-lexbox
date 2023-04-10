FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

COPY . .
# WORKDIR /src
# Copy the main source project files
# COPY */*.csproj ./
#
# move them into the proper sub folders, based on the name of the project
# RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore "LexBoxApi/LexBoxApi.csproj"
# COPY . .
# WORKDIR "/LexBoxApi"

RUN dotnet build "/LexBoxApi/LexBoxApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "/LexBoxApi/LexBoxApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# ENTRYPOINT ["dotnet", "LexBoxApi.dll"]
