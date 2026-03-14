# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

# COPY with apostrophe in path is unsupported by Docker's parser — copy everything at once
COPY . .

# Restore dependencies
RUN dotnet restore "Hackaton_INT20'26_Task/Hackaton_INT20'26_Task.csproj"

WORKDIR "/src/Hackaton_INT20'26_Task"

# Build
RUN dotnet build "Hackaton_INT20'26_Task.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Hackaton_INT20'26_Task.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Hackaton_INT20_26_Task.dll"]