# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
EXPOSE 80
EXPOSE 443
WORKDIR /src/backend
# Copy the main source project files
COPY */*.csproj *.sln ./
# move them into the proper sub folders, based on the name of the project
RUN for file in $(ls *.csproj); do dir=${file%.*} mkdir -p ${file%.*}/ && mv $file ${file%.*}/ && dotnet restore ${file%.*}/${file}; done

COPY . .
WORKDIR /src/backend/LexBoxApi
RUN mkdir /src/frontend
ENV DockerDev=true
CMD dotnet watch run -lp docker --property:InformationalVersion=dockerDev
