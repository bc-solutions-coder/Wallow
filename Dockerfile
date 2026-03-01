# syntax=docker/dockerfile:1.9-labs
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy only files needed for restore (better layer caching)
# When only source code changes, the restore layer stays cached
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["Foundry.sln", "./"]

# Copy all .csproj files preserving directory structure (requires BuildKit)
COPY --parents src/**/*.csproj ./
COPY --parents tests/**/*.csproj ./

RUN dotnet restore "Foundry.sln"

# Now copy everything and build
COPY . .
RUN dotnet build "src/Foundry.Api/Foundry.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ARG VERSION=0.0.0-local
RUN dotnet publish "src/Foundry.Api/Foundry.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false /p:Version=${VERSION}

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Foundry.Api.dll"]
