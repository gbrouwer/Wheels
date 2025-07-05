# 🛞 Wheels Motor Module

## Summary

The motor module forms the actuator layer of the *Wheels* robot system. It allows Unity to send **precise wheel control commands** to the Raspberry Pi over WebSockets, and to **visualize the motor response** through animated wheel meshes in the Unity scene. This enables real-time, bidirectional interaction between virtual and physical motor behaviors.

---

## 🚦 Architecture

### Components

| Component                   | Location      | Role                                                      |
|----------------------------|---------------|-----------------------------------------------------------|
| `motor_client.py`          | Raspberry Pi  | Connects to Unity and receives motor commands             |
| `Ordinary_Car`             | Raspberry Pi  | Hardware control layer for motor PWM via PCA9685          |
| `MotorCommandServer.cs`    | Unity          | WebSocket server listening for Pi connections             |
| `MotorCommandBehavior.cs`  | Unity          | Manages WebSocket sessions, sends JSON motor commands     |
| `MotorControlTester.cs`    | Unity          | Input controller (keyboard/UI) for testing motor control  |
| `MotorVisualController.cs` | Unity          | Rotates Unity wheel meshes to reflect current motor state |

---

## 🔁 Data Flow

```
[Unity User Input] ──▶ MotorControlTester.cs
        │
        ├─▶ MotorCommandServer.SendMotorCommand(d1, d2, d3, d4)
        │      └─▶ MotorCommandBehavior.Broadcast(JSON)
        │              └─▶ motor_client.py (WebSocket client)
        │                      └─▶ Ordinary_Car.set_motor_model(d1..4)
        │
        └─▶ MotorVisualController.ApplyMotorCommand(d1..4)
```

---

## 📡 Messaging Protocol

### Format

```json
{
  "duty1": 1000,
  "duty2": 1000,
  "duty3": 1000,
  "duty4": 1000
}
```

### Semantics

- Positive = forward
- Negative = reverse
- Zero = stop

---

## 🧠 Raspberry Pi Components

### `motor_client.py`

- Connects to Unity WebSocket server (`ws://<PC-IP>:9010/motor`)
- Retries connection every 2 seconds
- Parses motor command JSON and calls `set_motor_model(...)`
- Logs all events and received values

### `Ordinary_Car`

- Manages actual motor hardware via PCA9685
- Maps duty cycles to physical wheel channels
- Ensures clean stop on shutdown

---

## 🖥 Unity Components

### `MotorCommandServer.cs`

- WebSocket server startup on port 9010
- Registers behavior on `/motor`
- Broadcasts commands via `SendMotorCommand(...)`

### `MotorCommandBehavior.cs`

- Manages sessions
- Logs client connects/disconnects
- Converts and broadcasts motor JSON

### `MotorControlTester.cs`

- Handles keyboard input (`W`, `A`, `S`, `D`, `Space`)
- Sends a command once per press
- Automatically stops motors after 0.25s
- Rejects new commands while motors are active

### `MotorVisualController.cs`

- Rotates Unity wheel meshes to match motor duty direction and magnitude
- Smooth real-time animation per frame
- Keeps visuals synced with motor intent

---

## 🕹 Controls

| Key | Motion       | Duty Vector                            |
|-----|--------------|-----------------------------------------|
| `W` | Forward      | `(1000, 1000, 1000, 1000)`              |
| `S` | Backward     | `(-1000, -1000, -1000, -1000)`          |
| `A` | Turn Left    | `(-1500, -1500, 2000, 2000)`            |
| `D` | Turn Right   | `(2000, 2000, -1500, -1500)`            |
| `Space` | Stop     | `(0, 0, 0, 0)`                          |

---

## 🔁 Lifecycle

1. Raspberry Pi boots, runs `motor_client.py`
2. Unity runs scene with `MotorCommandServer`
3. User presses a key
4. Unity sends command → animates wheels → sends auto-stop after 0.25s

---

## 🔍 Troubleshooting

| Symptom                          | Cause                                       |
|----------------------------------|---------------------------------------------|
| Client crashes                   | Missing or renamed `motor.py` on Pi         |
| Skipped command                  | Lockout or cooldown active                  |
| Motors spin continuously         | No stop command sent                        |
| Wheels don’t rotate              | Visual controller not assigned              |
| No Pi connection                 | Unity server not running / wrong IP         |

---

## 🔮 Future Extensions

- Gamepad or joystick input
- AI agent-controlled driving
- Feedback from encoders or IMU for closed-loop control
- Replay of command sequences
- Curriculum-based motion training