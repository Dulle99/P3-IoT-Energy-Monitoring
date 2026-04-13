# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["visualization-service/VisualizationService/VisualizationService.csproj", "visualization-service/VisualizationService/"]
RUN dotnet restore "visualization-service/VisualizationService/VisualizationService.csproj"

COPY . .

WORKDIR /src/visualization-service/VisualizationService
RUN dotnet publish "VisualizationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VisualizationService.dll"]