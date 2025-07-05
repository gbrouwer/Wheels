#!/usr/bin/env python3
import json
import subprocess
import platform
import asyncio
from broadcasting_server import BroadcastingServer

MODULES_FILE = "/home/gbrouwer/Wheels/config/modules.json"

def load_modules():
    with open(MODULES_FILE, "r") as f:
        return json.load(f)

def am_i_pi():
    arch = platform.machine()
    return arch.startswith("arm") or arch.startswith("aarch")

def launch_module(module):
    cmd = f"{module['cmd']} --port {module['port']}" if "port" in module else module['cmd']
    print(f"[Orchestrator] Launching: {cmd} (host: {module.get('host', 'n/a')})")
    if module.get("sudo", False):
        cmd = f"sudo {cmd}"
    try:
        proc = subprocess.Popen(cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        print(f"[Orchestrator] Started process PID {proc.pid} for {module['name']}")
    except Exception as e:
        print(f"[Orchestrator] Failed to launch {module['name']}: {e}")

async def handle_module_message(sender, message):
    print(f"[Orchestrator] Received message: {message}")
    try:
        data = json.loads(message)
    except json.JSONDecodeError:
        print(f"[Orchestrator] ⚠️ Could not parse message: {message}")
        return

    module = data.get("module")
    status = data.get("status")
    global booted_modules, connected_unity_clients, actuator_clients_connected

    if module and status == "boot_success":
        booted_modules.add(module)
        print(f"[Orchestrator] Boot success: {module} ({len(booted_modules)}/{len(expected_modules)})")

    if module and status == "connected" and module in expected_unity_clients:
        connected_unity_clients.add(module)
        print(f"[Orchestrator] ✅ Unity sensor client connected: {module} ({len(connected_unity_clients)}/{len(expected_unity_clients)})")

        if connected_unity_clients == expected_unity_clients:
            print("[Orchestrator] 🎉 All Unity sensor clients connected. Launching Raspberry actuator clients.")
            launch_actuator_clients()
            await broadcasting_server.broadcast(json.dumps({
                "orchestrator": "pi",
                "status": "actuator_clients_started"
            }))

    if module and status == "actuator_connected":
        actuator_clients_connected.add(module)
        print(f"[Orchestrator] ✅ Raspberry actuator client connected to Unity server: {module} ({len(actuator_clients_connected)}/{len(expected_actuator_clients)})")

        if actuator_clients_connected == expected_actuator_clients:
            print("[Orchestrator] 🎉 All Raspberry actuator clients connected to Unity servers. Notifying Unity orchestrator.")
            await broadcasting_server.broadcast(json.dumps({
                "orchestrator": "pi",
                "status": "all_actuators_connected"
            }))

def launch_actuator_clients():
    modules = load_modules()
    actuator_clients = [
        module for module in modules
        if module["host"] == my_host and module.get("phase") == "actuator"
    ]

    for client in actuator_clients:
        print(f"[Orchestrator] Launching actuator client: {client['name']}")
        launch_module(client)

async def main():
    modules = load_modules()
    global my_host, expected_modules, booted_modules, expected_unity_clients, connected_unity_clients, expected_actuator_clients, actuator_clients_connected
    my_host = "pi" if am_i_pi() else "pc"
    print(f"[Orchestrator] Detected self as: {my_host.upper()}")

    expected_modules = {module["name"] for module in modules if module["host"] == my_host and module.get("phase", "sensor") != "actuator"}
    booted_modules = set()

    expected_unity_clients = {
        "ultrasonic_client",
        "infrared_client",
        "light_left_client",
        "light_right_client",
        "picamera_client"
    }
    connected_unity_clients = set()

    expected_actuator_clients = {
        "led_client",
        "motor_client",
        "servo_client",
        "speaker_client"
    }
    actuator_clients_connected = set()

    orchestrator_port = 9900
    global broadcasting_server
    broadcasting_server = BroadcastingServer(port=orchestrator_port)
    broadcasting_server.set_on_message(handle_module_message)
    broadcasting_task = asyncio.create_task(broadcasting_server.run())

    await asyncio.sleep(1)

    for module in modules:
        if module["host"] == my_host and module.get("phase", "sensor") != "actuator":
            launch_module(module)

    await broadcasting_task

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("[Orchestrator] Shutting down.")
