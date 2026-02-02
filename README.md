# DLMS MQTT Client gRPC

A .NET 8.0 gRPC service for communicating with DLMS (Device Language Message Specification) smart meters over MQTT protocol.

## Overview

This project provides a gRPC interface to interact with DLMS-compliant smart meters using MQTT as the transport layer. It supports reading meter data, profile generic data, executing scripts, and controlling meter disconnect functionality.

## Architecture

The project follows Clean Architecture principles with the following layers:

```
DlmsMqttExecutor/
├── DlmsMqttExecutor.Presentation   # gRPC service host, configuration
├── DlmsMqttExecutor.Application    # Business logic, services, DTOs
├── DlmsMqttExecutor.Domain         # Domain models
├── DlmsMqttExecutor.Infrastructure # DLMS client, MQTT client implementations
├── DlmsMqttExecutor.Protos         # Protocol buffer definitions
└── DlmsMqttExecutor.Tests          # Unit tests
```

## Features

- **Connect and Read**: Read DLMS objects from smart meters
- **Profile Generic**: Read profile generic data with time-based filtering
- **Script Execution**: Execute script tables on meters
- **Disconnect Control**: Control meter relay/disconnect switch
- **Connection Caching**: Efficient connection pooling with sliding expiration
- **MQTT Transport**: Communicate with meters via MQTT broker

## Prerequisites

- .NET 8.0 SDK
- MQTT Broker (e.g., Mosquitto, EMQX)
- Docker (optional, for containerized deployment)

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `LOG_MIN_LEVEL` | Minimum log level | `Debug` |
| `DLMS_MQTT_CLIENT_GRPC_AppSettings__DlmsCacheFolderPath` | Path for DLMS cache | `Cache` |
| `DLMS_MQTT_CLIENT_GRPC_AppSettings__DlmscClientChacheLimit` | Max cached connections | `1024` |
| `DLMS_MQTT_CLIENT_GRPC_AppSettings__DlmsClientCacheExpiration` | Cache expiration time | `00:00:10` |
| `DLMS_MQTT_CLIENT_GRPC_MqttSettings__Host` | MQTT broker host | `localhost` |
| `DLMS_MQTT_CLIENT_GRPC_MqttSettings__Port` | MQTT broker port | `1883` |
| `DLMS_MQTT_CLIENT_GRPC_MqttSettings__Username` | MQTT username | - |
| `DLMS_MQTT_CLIENT_GRPC_MqttSettings__Password` | MQTT password | - |

## Getting Started

### Running Locally

1. Clone the repository
2. Configure environment variables (copy `.env.example` to `.env` in `DlmsMqttExecutor.Presentation`)
3. Run the application:

```bash
cd DlmsMqttExecutor.Presentation
dotnet run
```

### Running with Docker

```bash
# Build the image
docker build -t dlms-mqtt-client-grpc .

# Run the container
docker run -p 5000:8080 \
  -e DLMS_MQTT_CLIENT_GRPC_MqttSettings__Host=your-mqtt-host \
  -e DLMS_MQTT_CLIENT_GRPC_MqttSettings__Port=1883 \
  dlms-mqtt-client-grpc
```

## gRPC API Reference

### Service: `DlmsProto`

#### `ConnectAndRead`
Read DLMS objects from a meter.

**Request**: `ConnectAndReadRequest`
- `args`: Connection arguments (including `-q` for MQTT topic, `-g` for logical names)
- `customArgs`: Optional filters (skip, take, from, to, cache control)

**Response**: `ConnectAndReadReply`
- `value`: JSON-structured meter data

#### `ConnectAndReadProfileGeneric`
Read profile generic data (load profiles, billing data).

**Request**: `ConnectAndReadRequest`
- Same as `ConnectAndRead`

**Response**: `ProfileGenericReply`
- `number_schema`: Schema for numeric columns
- `string_schema`: Schema for string columns
- `rows`: Profile data rows with timestamps

#### `ExecuteScriptTable`
Execute a script on the meter.

**Request**: `ExecuteScriptTableRequest`
- `args`: Connection arguments
- `scriptLn`: Script logical name
- `scriptId`: Script ID to execute

**Response**: `EmptyReply`

#### `SetDisconnectControl`
Control the meter's disconnect switch.

**Request**: `SetDisconnectControlRequest`
- `args`: Connection arguments
- `value`: `true` to connect, `false` to disconnect

**Response**: `EmptyReply`

## Connection Arguments

Common connection arguments passed in the `args` field:

| Argument | Description |
|----------|-------------|
| `-q` | MQTT topic for the meter |
| `-g` | Logical name(s) to read (format: `LN:index`) |
| `-c` | Client address |
| `-s` | Server address |
| `-a` | Authentication level |
| `-P` | Password/HLS secret |

## Development

### Building

```bash
dotnet build
```

### Running Tests

```bash
dotnet test
```

### Generating Proto Files

Proto files are automatically generated during build. The definitions are located in:
`DlmsMqttExecutor.Protos/Protos/dlms.proto`

## Dependencies

- [Gurux.DLMS](https://github.com/Gurux/Gurux.DLMS.Net) - DLMS/COSEM protocol implementation
- [Grpc.AspNetCore](https://www.nuget.org/packages/Grpc.AspNetCore) - gRPC for ASP.NET Core
- MQTT client libraries for MQTT transport

## License

See LICENSE file for details.
