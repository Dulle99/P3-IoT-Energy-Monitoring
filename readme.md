# P3 IoT Energy Monitoring

An IoT energy monitoring system built with **.NET**, **EdgeX Foundry**, **InfluxDB**, and **Grafana**.

This project simulates household energy readings, sends them through **EdgeX Foundry**, visualizes them in **Grafana**, and performs automated monitoring and reaction when power consumption exceeds a configured threshold.

---

## Table of Contents

- [Problem Abstraction](#problem-abstraction)
- [Project Goal](#project-goal)
- [Architecture Overview](#architecture-overview)
- [Main Components](#main-components)
- [Data Flow](#data-flow)
- [Repository Structure](#repository-structure)
- [Technologies Used](#technologies-used)
- [Prerequisites](#prerequisites)
- [How to Run the System](#how-to-run-the-system)
  - [1. Clone the Repositories](#1-clone-the-repositories)
  - [2. Start EdgeX Foundry](#2-start-edgex-foundry)
  - [3. Verify EdgeX Health](#3-verify-edgex-health)
  - [4. Register the Device Profile and Device](#4-register-the-device-profile-and-device)
  - [5. Start the Project Services](#5-start-the-project-services)
  - [6. Check Service Logs](#6-check-service-logs)
  - [7. Verify EdgeX Event Ingestion](#7-verify-edgex-event-ingestion)
  - [8. Verify InfluxDB](#8-verify-influxdb)
  - [9. Verify Grafana](#9-verify-grafana)
  - [10. Verify Core Command Manually](#10-verify-core-command-manually)
- [Expected End-to-End Behavior](#expected-end-to-end-behavior)
- [Command Flow](#command-flow)
- [Grafana Queries](#grafana-queries)
- [Troubleshooting](#troubleshooting)
- [Future Improvements](#future-improvements)

---

## Problem Abstraction

This project addresses the problem of **energy monitoring and automated reaction in a smart home environment**.

The abstract problem is:

- there is a source of energy readings,
- readings must be ingested through an IoT middleware platform,
- the system must store and visualize those readings,
- the system must detect suspicious or undesirable consumption patterns,
- once a configured condition is met, the system should automatically send a command that reduces future power usage.

In this implementation:

- a simulator acts as a smart meter,
- EdgeX Foundry acts as the IoT middleware,
- a visualization microservice writes readings into a time-series database,
- a monitoring microservice watches for repeated threshold violations,
- a command is sent back through EdgeX,
- the simulator changes its behavior and starts sending reduced values.

This demonstrates a complete closed-loop IoT workflow:

**measure -> detect -> decide -> command -> react**

---

## Project Goal

The goal of the project is to demonstrate a realistic IoT architecture where:

- readings are generated from a real dataset,
- data is ingested through EdgeX Foundry,
- services consume EdgeX events through MQTT,
- readings are persisted in InfluxDB,
- Grafana visualizes the readings,
- monitoring logic detects high energy usage,
- EdgeX Core Command triggers a command back to the device,
- the device reacts by enabling load shedding.

---

## Architecture Overview

The project uses a microservice-based design with EdgeX Foundry as the central IoT integration layer.

High-level architecture:

- **SensorSimulator** generates readings and receives control commands
- **EdgeX Foundry** ingests readings and distributes events
- **VisualizationService** consumes EdgeX events and writes to InfluxDB
- **MonitoringService** consumes EdgeX events, applies rules, and sends commands through EdgeX Core Command
- **InfluxDB** stores time-series data
- **Grafana** displays dashboards and charts

---

## Main Components

### 1. SensorSimulator

Responsibilities:

- reads a sample household power consumption dataset
- sends readings to `device-rest` in EdgeX
- exposes an HTTP endpoint that receives the `LoadShedSwitch` command
- modifies future readings when load shedding is enabled

### 2. EdgeX Foundry

Used services:

- `device-rest`
- `core-metadata`
- `core-data`
- `core-command`
- `mqtt-broker`

Responsibilities:

- accepts incoming device readings
- creates EdgeX events
- publishes events to the EdgeX MQTT broker
- forwards write commands back to the simulated device

### 3. VisualizationService

Responsibilities:

- subscribes to `edgex/events/#`
- parses EdgeX event envelopes
- filters events for `smart-meter-1`
- writes `globalActivePower`, `voltage`, and `globalIntensity` into InfluxDB

### 4. MonitoringService

Responsibilities:

- subscribes to `edgex/events/#`
- filters events for `smart-meter-1`
- monitors `globalActivePower`
- detects repeated threshold violations
- sends `LoadShedSwitch` to EdgeX Core Command
- avoids sending duplicate commands repeatedly once load shedding is already active

### 5. InfluxDB

Responsibilities:

- stores time-series readings in bucket `energy-bucket`

### 6. Grafana

Responsibilities:

- visualizes stored energy readings
- shows power, voltage, and intensity over time

---

## Data Flow

### Normal data flow

`SensorSimulator -> EdgeX device-rest -> EdgeX Core Data / MQTT events -> VisualizationService -> InfluxDB -> Grafana`

### Monitoring data flow

`SensorSimulator -> EdgeX device-rest -> EdgeX MQTT events -> MonitoringService -> EdgeX Core Command -> device-rest -> SensorSimulator`

### Closed-loop reaction

1. the simulator sends high `globalActivePower` values
2. MonitoringService detects repeated threshold violations
3. MonitoringService sends `LoadShedSwitch`
4. EdgeX Core Command forwards the command
5. SensorSimulator receives the command
6. future readings are reduced because load shedding is enabled

---

## Repository Structure

```text
P3-IoT-Energy-Monitoring
├── dataset
├── edgex-config
├── grafana
├── influxdb
├── monitoring-service
│   └── MonitoringService
├── sensor-simulator
│   └── SensorSimulator
├── visualization-service
│   └── VisualizationService
├── docker-compose.yml
└── README.md
```

---

## Technologies Used

- .NET 10
- ASP.NET Core / Minimal API
- EdgeX Foundry
- MQTT
- InfluxDB 2.x
- Grafana
- Docker / Docker Compose
- C#

---

## Prerequisites

Before running the system, make sure you have:

- **Docker Desktop**
- **Git**
- optional: **.NET 10 SDK** if you want to run services locally outside Docker

You also need a separate local clone of the **EdgeX Compose** repository.

> Note: this project does **not** include the entire `edgex-compose` repository inside itself. EdgeX is started from a separate folder/repository.

---

## How to Run the System

## 1. Clone the Repositories

### Clone this project

```powershell
git clone https://github.com/Dulle99/P3-IoT-Energy-Monitoring.git
```

### Clone EdgeX Compose

```powershell
cd C:\Projects
git clone https://github.com/edgexfoundry/edgex-compose.git
cd edgex-compose
git checkout v4.0
```

---

## 2. Start EdgeX Foundry

From the `edgex-compose` folder:

```powershell
cd C:\Projects\edgex-compose
docker compose -f docker-compose-no-secty.yml up -d
docker compose -f docker-compose-no-secty.yml ps
```

---

## 3. Verify EdgeX Health

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:59986/api/v3/ping"
Invoke-RestMethod -Method Get -Uri "http://localhost:59881/api/v3/ping"
Invoke-RestMethod -Method Get -Uri "http://localhost:59880/api/v3/ping"
Invoke-RestMethod -Method Get -Uri "http://localhost:59882/api/v3/ping"
```

These endpoints correspond to:

- `device-rest`
- `core-metadata`
- `core-data`
- `core-command`

All of them should respond successfully.

---

## 4. Register the Device Profile and Device

If EdgeX was reset or started from a clean state, register the custom profile and device again.

### Upload the device profile

```powershell
curl.exe -X POST -F "file=@C:/Projects/P3 IoT Energy Monitoring/edgex-config/smart-meter-profile.yaml" http://localhost:59881/api/v3/deviceprofile/uploadfile
```

### Create the device

```powershell
curl.exe -X POST -H "Content-Type: application/json" --data-binary "@C:/Projects/P3 IoT Energy Monitoring/edgex-config/smart-meter-device.json" http://localhost:59881/api/v3/device
```

### Verify profile and device registration

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:59881/api/v3/deviceprofile/name/smart-meter"
Invoke-RestMethod -Method Get -Uri "http://localhost:59881/api/v3/device/name/smart-meter-1"
Invoke-RestMethod -Method Get -Uri "http://localhost:59882/api/v3/device/name/smart-meter-1"
```

---

## 5. Start the Project Services

From the project root:

```powershell
cd "C:\Projects\P3 IoT Energy Monitoring"
docker compose up -d --build
docker compose ps
```

Expected services:

- `sensor-simulator`
- `visualization-service`
- `monitoring-service`
- `influxdb`
- `grafana`

---

## 6. Check Service Logs

### SensorSimulator

```powershell
docker compose logs -f sensor-simulator
```

### VisualizationService

```powershell
docker compose logs -f visualization-service
```

### MonitoringService

```powershell
docker compose logs -f monitoring-service
```

---

## 7. Verify EdgeX Event Ingestion

Count events for the smart meter:

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:59880/api/v3/event/count/device/name/smart-meter-1"
```

Read events for the smart meter:

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:59880/api/v3/event/device/name/smart-meter-1"
```

---

## 8. Verify InfluxDB

Health endpoint:

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:8086/health"
```

Open UI:

- `http://localhost:8086`

Expected bucket:

- `energy-bucket`

Expected measurement:

- `energy_readings`

Expected fields:

- `globalActivePower`
- `voltage`
- `globalIntensity`

---

## 9. Verify Grafana

Open:

- `http://localhost:3000`

Check that the dashboard displays:

- `globalActivePower`
- `voltage`
- `globalIntensity`

### Important time note

Originally the project used historical dataset timestamps.
Now the system stores **EdgeX event origin timestamps**, which are recent/current.

Because of that, Grafana should use a recent time range such as:

- **Last 5 minutes**
- **Last 15 minutes**
- **Last 1 hour**

If Grafana shows no data, the first thing to check is the dashboard time range.

---

## 10. Verify Core Command Manually

### Send `LoadShedSwitch = true` through EdgeX

```powershell
$body = @{
    loadShedSwitch = "true"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:59882/api/v3/device/name/smart-meter-1/LoadShedSwitch" `
  -ContentType "application/json" `
  -Body $body
```

### Send `LoadShedSwitch = false`

```powershell
$body = @{
    loadShedSwitch = "false"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:59882/api/v3/device/name/smart-meter-1/LoadShedSwitch" `
  -ContentType "application/json" `
  -Body $body
```

Expected result:

- EdgeX returns `statusCode: 200`
- `sensor-simulator` receives the command
- simulator toggles load shedding state

---

## Expected End-to-End Behavior

When the system is working correctly:

1. `sensor-simulator` reads the dataset and sends readings to EdgeX
2. EdgeX registers the readings and emits events on `edgex/events/#`
3. `visualization-service` consumes those events and writes values into InfluxDB
4. Grafana shows the readings in near real time
5. `monitoring-service` detects repeated threshold violations
6. `monitoring-service` sends `LoadShedSwitch` through EdgeX Core Command
7. EdgeX forwards the command back to `sensor-simulator`
8. `sensor-simulator` enables load shedding
9. future values are reduced

---

## Command Flow

### Direct command to SensorSimulator

```powershell
Invoke-RestMethod -Method Put -Uri "http://localhost:7070/api/LoadShedSwitch" -ContentType "application/json" -Body '{"loadShedSwitch":"true"}'
```

This is useful for testing the simulator itself without going through EdgeX.

### Full command through EdgeX

```powershell
$body = @{
    loadShedSwitch = "true"
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:59882/api/v3/device/name/smart-meter-1/LoadShedSwitch" `
  -ContentType "application/json" `
  -Body $body
```

This is the real project path:

- MonitoringService uses this path automatically
- EdgeX forwards it through `device-rest`
- SensorSimulator receives it as the simulated end device

---

## Grafana Queries

Example query for `globalActivePower`:

```flux
from(bucket: "energy-bucket")
  |> range(start: v.timeRangeStart, stop: v.timeRangeStop)
  |> filter(fn: (r) => r["_measurement"] == "energy_readings")
  |> filter(fn: (r) => r["_field"] == "globalActivePower")
  |> filter(fn: (r) => r["deviceId"] == "smart-meter-1")
```

Example query for `voltage`:

```flux
from(bucket: "energy-bucket")
  |> range(start: v.timeRangeStart, stop: v.timeRangeStop)
  |> filter(fn: (r) => r["_measurement"] == "energy_readings")
  |> filter(fn: (r) => r["_field"] == "voltage")
  |> filter(fn: (r) => r["deviceId"] == "smart-meter-1")
```

Example query for `globalIntensity`:

```flux
from(bucket: "energy-bucket")
  |> range(start: v.timeRangeStart, stop: v.timeRangeStop)
  |> filter(fn: (r) => r["_measurement"] == "energy_readings")
  |> filter(fn: (r) => r["_field"] == "globalIntensity")
  |> filter(fn: (r) => r["deviceId"] == "smart-meter-1")
```

---

## Troubleshooting

### EdgeX returns `Unauthorized`

Reset and restart non-secure EdgeX:

```powershell
cd C:\Projects\edgex-compose
docker compose -f docker-compose-no-secty.yml down -v
docker compose -f docker-compose-no-secty.yml up -d
```

Then register the profile and device again.

### Device or profile missing after restart

Re-register:

- the custom device profile
- the custom device

### InfluxDB bucket not found

Make sure the configured bucket is:

- `energy-bucket`

Also verify that `visualization-service` uses the same bucket in its environment variables.

### Grafana shows no data

Check:

- InfluxDB contains points
- dashboard time range is recent
- Flux queries use `v.timeRangeStart` and `v.timeRangeStop`

### Monitoring keeps sending the same command

This can happen if the threshold is set too low for testing.
Example:

- if threshold is `2`
- and load shedding reduces power to `2.5`
- then the system still sees readings above threshold

For final testing, use realistic thresholds or ensure reduced readings go below the configured threshold.

---

## Future Improvements

- move noisy logs to `Debug` level
- clean up variable naming (`LoadShedEnable` -> `LoadShedEnabled`)
- improve duplicate command suppression logic
- add screenshots of Grafana and EdgeX flow
- add architecture diagram
- improve dashboard styling
- optionally add a dedicated actuator service instead of combining actuator behavior into the simulator

---

## Final Notes

This project demonstrates a full closed-loop IoT control pipeline:

- simulated sensor readings
- EdgeX ingestion
- event-driven service consumption
- persistence into InfluxDB
- Grafana visualization
- rule-based monitoring
- command dispatch through EdgeX Core Command
- reaction by the device simulator

The final result is not only monitoring, but also **automated corrective action**.
