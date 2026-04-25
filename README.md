# P3 IoT Energy Monitoring

An IoT energy monitoring system built with **EdgeX Foundry**, **.NET**, **MQTT**, **InfluxDB**, **Grafana**, and **Docker Compose**.

The project simulates a smart electricity meter that reads real household power consumption data from a dataset file, sends those readings into EdgeX Foundry, stores them in InfluxDB, visualizes them in Grafana, and reacts automatically when power consumption exceeds a configured threshold.

---

## Table of Contents

- [Problem Abstraction](#problem-abstraction)
- [Dataset](#dataset)
- [Project Goal](#project-goal)
- [Architecture Overview](#architecture-overview)
- [Main Components](#main-components)
- [Data Flow](#data-flow)
- [Repository Structure](#repository-structure)
- [Technologies Used](#technologies-used)
- [Prerequisites](#prerequisites)
- [How to Run the System](#how-to-run-the-system)
- [Useful URLs](#useful-urls)
- [Monitoring REST API](#monitoring-rest-api)
- [Grafana Dashboard](#grafana-dashboard)
- [Testing the System](#testing-the-system)
- [Troubleshooting](#troubleshooting)
- [Final Notes](#final-notes)

---

## Problem Abstraction

The project models a simplified **smart home energy monitoring and control system**.

In a real IoT environment, energy meters continuously produce readings such as active power, voltage, and current intensity. These readings must be collected, processed, stored, visualized, and monitored. When the system detects high consumption for a defined period of time, it should react by sending a command back to the device or actuator.

This project abstracts that problem as a closed-loop IoT workflow:

```text
measure -> ingest -> distribute -> store -> visualize -> monitor -> command -> react
```

The system does not only collect sensor data. It also performs an automated corrective action when high power consumption is detected.

---

## Dataset

The project uses the **Individual Household Electric Power Consumption** dataset.

Dataset source:

- Kaggle: https://www.kaggle.com/datasets/uciml/electric-power-consumption-data-set/data

The original dataset contains measurements of electric power consumption in a household over time.

For this project, a smaller sample file is included in the repository to keep the project lightweight and easy to run:

```text
dataset/household_power_consumption_sample.txt
```

Used dataset columns:

- `Global_active_power`
- `Voltage`
- `Global_intensity`

These values are mapped to EdgeX device resources:

| Dataset column | EdgeX resource | Meaning |
|---|---|---|
| `Global_active_power` | `globalActivePower` | Household active power consumption in kW |
| `Voltage` | `voltage` | Voltage in V |
| `Global_intensity` | `globalIntensity` | Current intensity in A |

---

## Project Goal

The goal of the project is to demonstrate a complete IoT service-oriented architecture where:

- a simulator reads real sensor data from a file,
- data is sent to EdgeX Foundry through a Device Service,
- EdgeX publishes events through MQTT,
- a Visualization microservice stores data in InfluxDB,
- Grafana displays the stored time-series readings,
- a Monitoring microservice consumes the same MQTT events,
- Monitoring applies a configurable rule through its REST API,
- Monitoring sends commands through EdgeX Core Command,
- the simulated device reacts by enabling or disabling load shedding.

---

## Architecture Overview

The system uses EdgeX Foundry as the IoT middleware layer.

```text
                                +----------------+
                                |    Grafana     |
                                +-------^--------+
                                        |
                                        |
+------------------+       +------------+------------+
| SensorSimulator  | ----> | EdgeX Foundry / MQTT    |
| smart-meter-1    |       | device-rest/core-data   |
+--------^---------+       +------------+------------+
         |                              |
         |                              |
         |                    +---------+----------+
         |                    | Visualization     |
         |                    | Service           |
         |                    +---------+----------+
         |                              |
         |                              v
         |                        +-----+------+
         |                        | InfluxDB   |
         |                        +------------+
         |
         |                    +----------------+
         +------------------- | Monitoring     |
                              | Service        |
                              +--------+-------+
                                       |
                                       v
                              EdgeX Core Command
```

---

## Main Components

### SensorSimulator

The `SensorSimulator` acts as a simulated smart electricity meter.

Responsibilities:

- reads household power consumption readings from a dataset file,
- sends readings to EdgeX `device-rest`,
- exposes an endpoint used by EdgeX write commands,
- receives `LoadShedSwitch` commands,
- changes future readings when load shedding is enabled.

When load shedding is enabled, high values are reduced before being sent again.

---

### EdgeX Foundry

EdgeX Foundry is used as the IoT middleware platform.

Used EdgeX services:

- `device-rest`
- `core-metadata`
- `core-data`
- `core-command`
- `edgex-mqtt-broker`

Responsibilities:

- receives device readings,
- stores EdgeX events,
- publishes events to MQTT,
- forwards commands back to the simulated device.

---

### VisualizationService

The Visualization microservice represents the northbound/fog/cloud integration layer.

Responsibilities:

- subscribes to the EdgeX MQTT topic `edgex/events/#`,
- parses EdgeX event payloads,
- filters events from `smart-meter-1`,
- writes readings to InfluxDB.

InfluxDB measurement:

```text
energy_readings
```

Stored fields:

- `globalActivePower`
- `voltage`
- `globalIntensity`

---

### MonitoringService

The Monitoring microservice also subscribes to the EdgeX MQTT topic `edgex/events/#`.

Responsibilities:

- receives EdgeX readings through MQTT,
- exposes a REST API for monitoring rule configuration,
- evaluates `globalActivePower`,
- detects repeated threshold violations,
- sends `LoadShedSwitch` commands through EdgeX Core Command,
- avoids sending duplicate commands repeatedly while the alarm is already active,
- provides manual endpoints for enabling and disabling load shedding.

Default rule:

```text
Device: smart-meter-1
Resource: globalActivePower
Threshold: 4.5
Required consecutive readings: 3
Command: LoadShedSwitch
```

---

### InfluxDB

InfluxDB stores time-series readings.

Default configuration:

```text
Organization: p3-org
Bucket: energy-bucket
Token: super-secret-token
```

---

### Grafana

Grafana is used for visualization.

The project includes Grafana provisioning files so the datasource and dashboard can be loaded automatically when Grafana starts.

---

## Data Flow

### Normal data flow

```text
SensorSimulator
    -> EdgeX device-rest
    -> EdgeX Core Data
    -> EdgeX MQTT Broker
    -> VisualizationService
    -> InfluxDB
    -> Grafana
```

### Monitoring and command flow

```text
SensorSimulator
    -> EdgeX device-rest
    -> EdgeX MQTT Broker
    -> MonitoringService
    -> EdgeX Core Command
    -> device-rest
    -> SensorSimulator
```

### Closed-loop behavior

1. SensorSimulator sends readings from the dataset.
2. EdgeX receives readings through `device-rest`.
3. EdgeX publishes events through MQTT.
4. VisualizationService writes readings into InfluxDB.
5. Grafana displays the data.
6. MonitoringService detects high power usage.
7. MonitoringService sends `LoadShedSwitch = true` through EdgeX Core Command.
8. SensorSimulator receives the command.
9. SensorSimulator enables load shedding.
10. Future power values are reduced.
11. MonitoringService detects that readings returned to a normal range.

---

## Repository Structure

```text
P3-IoT-Energy-Monitoring
├── dataset
│   └── household_power_consumption_sample.txt
├── edgex-config
│   ├── smart-meter-device.json
│   └── smart-meter-profile.yaml
├── grafana
│   ├── dashboards
│   │   └── energy-monitoring-dashboard.json
│   └── provisioning
│       ├── dashboards
│       │   └── dashboard-provider.yml
│       └── datasources
│           └── influxdb.yml
├── monitoring-service
│   └── MonitoringService
├── sensor-simulator
│   └── SensorSimulator
├── visualization-service
│   └── VisualizationService
├── scripts
│   └── register-edgex-device.ps1
├── docker-compose.yml
├── README.md
└── .gitignore
```

The `edgex-compose` repository is cloned locally inside this project folder, but it is ignored by Git and is not part of this repository.

---

## Technologies Used

- .NET 10
- ASP.NET Core
- EdgeX Foundry
- MQTT
- InfluxDB 2.x
- Grafana
- Docker
- Docker Compose
- PowerShell
- C#

---

## Prerequisites

Before running the system, install:

- Docker Desktop
- Git
- PowerShell
- .NET SDK only if you want to run or build services locally outside Docker

Docker Desktop must be running before executing the commands.

---

## How to Run the System

The following steps assume a Windows PowerShell environment.

### 1. Clone this repository

```powershell
git clone https://github.com/Dulle99/P3-IoT-Energy-Monitoring.git
cd P3-IoT-Energy-Monitoring
```

---

### 2. Clone EdgeX Compose inside the project folder

```powershell
git clone https://github.com/edgexfoundry/edgex-compose.git
cd edgex-compose
git checkout v4.0
```

---

### 3. Start EdgeX Foundry

From inside the `edgex-compose` folder:

```powershell
docker compose -f docker-compose-no-secty.yml up -d
```

Verify that EdgeX containers are running:

```powershell
docker compose -f docker-compose-no-secty.yml ps
```

Return to the project root:

```powershell
cd ..
```

---

### 4. Register the custom EdgeX device profile and device

The project includes a PowerShell script that automatically registers:

- the `smart-meter` device profile,
- the `smart-meter-1` simulated device.

Run it from the project root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\register-edgex-device.ps1
```

Expected result:

```text
Device profile uploaded.
Device created.
Core Command can see 'smart-meter-1'.
Registration completed successfully.
```

If the profile or device already exists, the script skips that step.

---

### 5. Start the project services

From the project root:

```powershell
docker compose up -d --build
```

Check running containers:

```powershell
docker compose ps
```

Expected project containers:

```text
sensor-simulator
visualization-service
monitoring-service
influxdb
grafana
```

---

## Useful URLs

| Service | URL |
|---|---|
| Grafana | http://localhost:3000 |
| InfluxDB | http://localhost:8086 |
| MonitoringService Swagger | http://localhost:8082/swagger |
| VisualizationService | http://localhost:8081 |
| SensorSimulator command endpoint | http://localhost:7070/api/LoadShedSwitch |
| EdgeX device-rest | http://localhost:59986 |
| EdgeX core-metadata | http://localhost:59881 |
| EdgeX core-data | http://localhost:59880 |
| EdgeX core-command | http://localhost:59882 |

---

## Monitoring REST API

Swagger UI is available at:

```text
http://localhost:8082/swagger
```

Available endpoints:

```text
GET  /api/monitoring/rule
PUT  /api/monitoring/rule
POST /api/monitoring/rule/reset-counter
POST /api/monitoring/rule/reset-defaults
POST /api/monitoring/rule/load-shed/on
POST /api/monitoring/rule/load-shed/off
```

### Get current monitoring rule

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:8082/api/monitoring/rule"
```

### Update monitoring rule

Example:

```powershell
$body = @{
    threshold = 4.5
    requiredConsecutiveReadings = 3
} | ConvertTo-Json

Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:8082/api/monitoring/rule" `
  -ContentType "application/json" `
  -Body $body
```

### Reset monitoring counter

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8082/api/monitoring/rule/reset-counter"
```

### Reset rule to default values

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8082/api/monitoring/rule/reset-defaults"
```

### Enable load shedding manually

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8082/api/monitoring/rule/load-shed/on"
```

### Disable load shedding manually

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8082/api/monitoring/rule/load-shed/off"
```

The `load-shed/on` and `load-shed/off` endpoints send commands through EdgeX Core Command, not directly to the simulator.

---

## Grafana Dashboard

Open Grafana:

```text
http://localhost:3000
```

Default credentials:

```text
admin / admin
```

The project contains Grafana provisioning files:

```text
grafana/provisioning/datasources/influxdb.yml
grafana/provisioning/dashboards/dashboard-provider.yml
grafana/dashboards/energy-monitoring-dashboard.json
```

Grafana should automatically load:

- InfluxDB datasource,
- Energy Monitoring dashboard.

If the dashboard does not show data immediately, set the time range to:

```text
Last 5 minutes
Last 15 minutes
Last 1 hour
```

The system writes recent/current EdgeX event timestamps, so a recent time range should be used.

---

## Testing the System

### 1. Check service logs

SensorSimulator:

```powershell
docker compose logs -f sensor-simulator
```

VisualizationService:

```powershell
docker compose logs -f visualization-service
```

MonitoringService:

```powershell
docker compose logs -f monitoring-service
```

---

### 2. Verify EdgeX event ingestion

Count events for the simulated smart meter:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:59880/api/v3/event/count/device/name/smart-meter-1"
```

Read events:

```powershell
Invoke-RestMethod `
  -Method Get `
  -Uri "http://localhost:59880/api/v3/event/device/name/smart-meter-1"
```

---

### 3. Verify InfluxDB

Health check:

```powershell
Invoke-RestMethod http://localhost:8086/health
```

Query data from the InfluxDB container:

```powershell
docker exec -it influxdb influx query `
'from(bucket: "energy-bucket")
  |> range(start: -1h)
  |> filter(fn: (r) => r._measurement == "energy_readings")
  |> limit(n: 20)' `
--org p3-org `
--token super-secret-token
```

---

### 4. Verify automatic monitoring reaction

Reset rule to defaults:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8082/api/monitoring/rule/reset-defaults"
```

Disable load shedding before a fresh test:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri "http://localhost:8082/api/monitoring/rule/load-shed/off"
```

Restart the simulator:

```powershell
docker compose restart sensor-simulator
```

Watch MonitoringService logs:

```powershell
docker compose logs -f monitoring-service
```

Expected behavior:

```text
[THRESHOLD EXCEEDED]
[THRESHOLD EXCEEDED]
[THRESHOLD EXCEEDED]
ALARM TRIGGERED
Successfully sent command to EdgeX
Reading back to normal: 2.5. Resetting consecutive count.
```

This means:

- high readings were detected,
- the rule condition was satisfied,
- MonitoringService sent a command through EdgeX,
- SensorSimulator enabled load shedding,
- future readings were reduced.

---

### 5. Verify command reception in SensorSimulator

```powershell
docker compose logs --tail=100 sensor-simulator
```

Expected messages:

```text
Load shed command received. LoadShedEnabled set to True
Load shed command received. LoadShedEnabled set to False
```

---


## Direct SensorSimulator Command Test

This test bypasses EdgeX and calls the simulator directly.

Enable load shedding:

```powershell
Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:7070/api/LoadShedSwitch" `
  -ContentType "text/plain" `
  -Body "true"
```

Disable load shedding:

```powershell
Invoke-RestMethod `
  -Method Put `
  -Uri "http://localhost:7070/api/LoadShedSwitch" `
  -ContentType "text/plain" `
  -Body "false"
```

This is useful only for debugging the simulator. The real project command path goes through EdgeX Core Command.

---

## Stopping the System

Stop only the project services:

```powershell
docker compose down
```

Stop EdgeX services:

```powershell
cd .\edgex-compose
docker compose -f docker-compose-no-secty.yml down
cd ..
```

---

## Clean Runtime Data

If you want to reset local runtime data:

```powershell
docker compose down

Remove-Item -Recurse -Force .\grafana\data -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\influxdb\data -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\influxdb\config -ErrorAction SilentlyContinue
```

Then start again:

```powershell
docker compose up -d --build
```

---

## Troubleshooting

### EdgeX network does not exist

If project services fail with:

```text
network edgex_edgex-network declared as external, but could not be found
```

Start EdgeX first:

```powershell
cd .\edgex-compose
docker compose -f docker-compose-no-secty.yml up -d
cd ..
```

Then run:

```powershell
docker compose up -d --build
```

---

### Device profile or device missing

Run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\register-edgex-device.ps1
```

---

### InfluxDB does not start correctly

If InfluxDB logs show a stale configuration or a conflict such as:

```text
config name "default" already exists
```

reset local InfluxDB runtime folders:

```powershell
docker compose down

Remove-Item -Recurse -Force .\influxdb\data -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force .\influxdb\config -ErrorAction SilentlyContinue

docker compose up -d influxdb
```

Check health:

```powershell
Invoke-RestMethod http://localhost:8086/health
```

---

### Grafana dashboard shows no data

Check:

1. `visualization-service` logs show successful writes.
2. InfluxDB is healthy.
3. Grafana time range is recent, for example `Last 15 minutes`.
4. The datasource is named `InfluxDB` and uses `uid: influxdb`.

---

### Monitoring keeps sending commands repeatedly

This can happen during testing if the threshold is set too low.

Example:

```text
threshold = 2
load shedding value = 2.5
```

Since `2.5 > 2`, the system still sees readings above threshold.

For the default demo, use:

```text
threshold = 4.5
requiredConsecutiveReadings = 3
```

---

## Final Notes

This project demonstrates a full closed-loop IoT control pipeline:

```text
dataset reading
    -> EdgeX ingestion
    -> MQTT event distribution
    -> InfluxDB persistence
    -> Grafana visualization
    -> rule-based monitoring
    -> EdgeX Core Command
    -> simulated device reaction
```

The final result is not only data monitoring, but also an automated reaction based on sensor readings.
