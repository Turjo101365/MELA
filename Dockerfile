# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["MelaFair.Core/MelaFair.Core.csproj", "MelaFair.Core/"]
COPY ["MelaFair.Web/MelaFair.Web.csproj", "MelaFair.Web/"]
RUN dotnet restore "MelaFair.Web/MelaFair.Web.csproj"

# Copy remaining source code
COPY MelaFair.Core/ MelaFair.Core/
COPY MelaFair.Web/ MelaFair.Web/

# Build and publish application
WORKDIR "/src/MelaFair.Web"
RUN dotnet publish "MelaFair.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ensure SQL artifact scripts are available for DbInitializer
COPY MelaFair.Web/Database ./Database

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_ENVIRONMENT=Development
EXPOSE 8080

ENTRYPOINT ["dotnet", "MelaFair.Web.dll"]
