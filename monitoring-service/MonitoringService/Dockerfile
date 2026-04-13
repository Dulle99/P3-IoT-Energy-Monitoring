# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["monitoring-service/MonitoringService/MonitoringService.csproj", "monitoring-service/MonitoringService/"]
RUN dotnet restore "monitoring-service/MonitoringService/MonitoringService.csproj"

COPY . .

WORKDIR /src/monitoring-service/MonitoringService
RUN dotnet publish "MonitoringService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MonitoringService.dll"]