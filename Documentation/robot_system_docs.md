# Distributed Raspberry Pi Robot System Documentation

## 🔎 Overview

This project implements a **modular, decentralized real-time control and sensing architecture** for a robot based on a Raspberry Pi. It is designed around **independent, self-contained modules** for each sensor and actuator, each communicating over **WebSockets**. The system supports both direct 1-to-1 sensor-to-PC communication and a local orchestration layer that provides coordination, logging, and control for all modules on the same device.

## 🌐 System Architecture

### Key high-level concepts

- **Modularization** — Each hardware component is implemented as a standalone Python script with its own WebSocket server or client.
- **Orchestrator per device** — A Python process on each major device (Pi or PC) launches modules, coordinates their operation, and hosts a broadcasting server.
- **Broadcasting channel** — Modules send boot status, heartbeats, and logs to the orchestrator through a shared WebSocket connection.
- **Direct communication** — Sensor data or actuator commands are sent directly between Unity/PC and each module.
- **Configuration-driven deployment** — Ports and module info are set in `modules.json`.

## 📦 Major Components

### ✅ Orchestrator

- Starts the broadcasting server.
- Launches local modules based on `modules.json`.
- Receives module statuses via broadcasting channel.

### ✅ Broadcasting Server

- WebSocket server inside orchestrator.
- Accepts connections from modules.
- Broadcasts messages to all modules.

### ✅ Broadcasting Client

- Runs in each module script.
- Connects to orchestrator broadcasting server.
- Sends boot statuses, heartbeats, and logs.

### ✅ Logger

- Logs timestamped messages to console and file.

### ✅ Sensor & Actuator Modules

- Each module runs independently.
- Connects to broadcasting server.
- Sends boot status.
- Logs runtime info.

## 📂 modules.json

Example entry:
```json
{
  "name": "ultrasonic_sensor",
  "host": "pi",
  "cmd": "python3 /home/gbrouwer/Wheels/src/ultrasonic_sensor.py",
  "type": "server",
  "port": 6603,
  "sudo": false
}
```

## 🛠 orchestrator.py

- Detects host type.
- Starts broadcasting server.
- Waits for server to be ready.
- Launches modules with `--port` argument.
- Handles messages received from modules.

## 🛠 broadcasting_server.py

- WebSocket server.
- Tracks connected clients.
- Calls orchestrator’s on-message handler.
- Rebroadcasts messages.

## 🛠 broadcasting_client.py

- Connects to orchestrator’s broadcasting server.
- Calls `on_connect_callback` when connected.
- Sends messages to orchestrator.

## 🛠 logger.py

- Logs to console and file with timestamps.

## 📂 Module Script Details

### ultrasonic_sensor.py
- Reads ultrasonic distance.
- WebSocket server streams data.
- Broadcasting client sends boot status.
- Uses asyncio.gather to run server and client concurrently.

### infrared_sensor.py
- Reads infrared sensor.
- Same design as ultrasonic_sensor.py.

### light_sensor.py
- Reads ADC-connected light sensor.
- Matches architecture of other sensors.

### picamera.py
- Streams PiCamera images.
- Encodes frames to JPEG + base64.

## 📦 Command-Line Port Injection

- Orchestrator appends `--port <value>` when launching each module.
- Modules use argparse to parse:
```python
parser = argparse.ArgumentParser()
parser.add_argument("--port", type=int, required=True)
args = parser.parse_args()
server = MySensorServer(port=args.port)
```

## 🧪 Testing & Debugging

- Use module logs in `/home/gbrouwer/Wheels/logs/`.
- Run `test_heartbeat_client.py` to verify broadcasting.
- Check orchestrator logs for module boot statuses.

## 🚀 Adding New Modules

1. Write module script matching existing sensors.
2. Add module entry in `modules.json`.
3. The orchestrator will launch the module with correct port.

## 🛡️ Best Practices

- Never hardcode ports in scripts.
- Keep orchestrator, modules.json, and module scripts in sync.
- Monitor logs for runtime health.

🎉 **You now have a complete reference for your Raspberry Pi robot system!**

