FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
EXPOSE 80
EXPOSE 443
WORKDIR /src/backend
COPY . .
WORKDIR /src/backend/LexBoxApi
RUN mkdir /src/frontend

CMD dotnet watch run -lp docker
