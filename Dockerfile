# Stage 1: Build Angular Frontend
FROM node:20-alpine as angular_build
WORKDIR /app/angular-client
COPY angular-client/package.json angular-client/package-lock.json ./
RUN npm cache clean --force
RUN rm -rf node_modules
RUN npm install
COPY angular-client/. ./
RUN npm run build -- --configuration production

# Stage 2: Build .NET Backend
FROM mcr.microsoft.com/dotnet/sdk:9.0 as dotnet_build
WORKDIR /app

# Copy solution and project files first for layer caching
COPY dotnet_server/ChattyMcChatface.sln ./dotnet_server/
COPY dotnet_server/ChattyMcChatface.Api/ChattyMcChatface.Api.csproj ./dotnet_server/ChattyMcChatface.Api/
COPY dotnet_server/ChattyMcChatface.Core/ChattyMcChatface.Core.csproj ./dotnet_server/ChattyMcChatface.Core/
COPY dotnet_server/ChattyMcChatface.Data/ChattyMcChatface.Data.csproj ./dotnet_server/ChattyMcChatface.Data/
COPY dotnet_server/ChattyMcChatface.Tests.Unit/ChattyMcChatface.Tests.Unit.csproj ./dotnet_server/ChattyMcChatface.Tests.Unit/
COPY dotnet_server/ChattyMcChatface.Tests.Integration/ChattyMcChatface.Tests.Integration.csproj ./dotnet_server/ChattyMcChatface.Tests.Integration/

# Restore dependencies
RUN dotnet restore ./dotnet_server/ChattyMcChatface.sln

# Copy the rest of the source code
COPY dotnet_server/. ./dotnet_server/

# Publish the .NET application
WORKDIR /app/dotnet_server/ChattyMcChatface.Api
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app
EXPOSE 8080

# Copy built .NET backend from build stage
COPY --from=dotnet_build /app/publish .

# Copy built Angular frontend from build stage into wwwroot
COPY --from=angular_build /app/angular-client/dist/angular-client/browser ./wwwroot

# Set environment variable for ASP.NET Core port
ENV ASPNETCORE_URLS=http://+:8080

# WARNING: TEMPORARY DEBUGGING STEP - EXPOSES SECRETS IN LOGS
# This entrypoint modification prints the content of the mounted secrets file.
# REMOVE THIS MODIFICATION IMMEDIATELY AFTER VERIFYING THE VOLUME MOUNT.
ENTRYPOINT ["sh", "-c", "echo '--- START appsettings.Docker.json ---' && cat /app/appsettings.Docker.json && echo '--- END appsettings.Docker.json ---' && echo 'Starting application...' && dotnet ChattyMcChatface.Api.dll"]