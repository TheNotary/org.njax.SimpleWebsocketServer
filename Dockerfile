# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

COPY *.sln ./
COPY WebsocketEdu/*.csproj ./WebsocketEdu/
COPY WebsocketEduTest/*.csproj ./WebsocketEduTest/
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out ./out
# WORKDIR /app/WebsocketEdu
# ENTRYPOINT [ "dotnet", "run", "-p", "WebsocketEdu" ]

WORKDIR /app/out
ENTRYPOINT [ "dotnet", "WebsocketEdu.dll"]
