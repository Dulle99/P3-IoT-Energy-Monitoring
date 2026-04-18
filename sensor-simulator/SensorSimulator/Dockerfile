FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["sensor-simulator/SensorSimulator/SensorSimulator.csproj", "sensor-simulator/SensorSimulator/"]
RUN dotnet restore "sensor-simulator/SensorSimulator/SensorSimulator.csproj"

COPY . .

WORKDIR /src/sensor-simulator/SensorSimulator
RUN dotnet publish "SensorSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SensorSimulator.dll"]