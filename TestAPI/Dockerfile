# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

# Set environment variable for Kestrel
ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

# SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy and restore project
COPY ["TestAPI/TestAPI.csproj", "TestAPI/"]
RUN dotnet restore "./TestAPI/TestAPI.csproj"

# Copy the rest of the code
COPY . .

# Build the app
WORKDIR "/src/TestAPI"
RUN dotnet build "TestAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TestAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "TestAPI.dll"]
