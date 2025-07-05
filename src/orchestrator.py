#!/usr/bin/env python3
import json
import subprocess
import platform
import asyncio
import websockets
from broadcasting_server import BroadcastingServer

MODULES_FILE = "/home/gbrouwer/Wheels/config/modules.json"

def load_modules():
    with open(MODULES_FILE, "r") as f:
        return json.load(f)

def am_i_pi():
    arch = platform.machine()
    return arch.startswith("arm") or arch.startswith("aarch")

def am_i_pc():
    return not am_i_pi()

def launch_module(module):
    print(f"[Orchestrator] Launching: {module['cmd']} (host: {module['host']})")
    cmd = f"{module['cmd']} --port {module['port']}"
    if module.get("sudo", False):
        cmd = f"sudo {cmd}"
    try:
        proc = subprocess.Popen(cmd, shell=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        print(f"[Orchestrator] Started process PID {proc.pid} for {module['name']}")
    except Exception as e:
        print(f"[Orchestrator] Failed to launch {module['name']}: {e}")

async def handle_module_message(sender, message):
    print(f"[Orchestrator] Received message from module: {message}")
    try:
        data = json.loads(message)
    except json.JSONDecodeError:
        print(f"[Orchestrator] ⚠️ Could not parse message: {message}")
        return

    module = data.get("module")
    status = data.get("status")
    orchestrator_status = data.get("status")
    global booted_modules, sent_connected, received_connected

    if module and status == "boot_success":
        booted_modules.add(module)
        print(f"[Orchestrator] Boot success reported by {module}. {len(booted_modules)}/{len(expected_modules)} booted.")

        if booted_modules == expected_modules and not sent_connected:
            print("[Orchestrator] ✅ All expected modules booted. Sending Connected signal.")
            await broadcasting_server.broadcast(json.dumps({
                "orchestrator": my_host,
                "status": "Connected"
            }))
            globals()["sent_connected"] = True

    if orchestrator_status == "Connected" and not received_connected:
        print("[Orchestrator] 🔔 Received Connected from Unity orchestrator.")
        globals()["received_connected"] = True

        if sent_connected:
            print("[Orchestrator] 🎉 Both orchestrators Connected! Ready for phase 2.")
            # Place phase 2 launch logic here

async def connect_to_unity_orchestrator():
    unity_ip = "192.168.178.237"  # your Unity PC's IP
    uri = f"ws://{unity_ip}:9901"
    while True:
        try:
            print(f"[OrchestratorClient] Trying to connect to Unity orchestrator at {uri}")
            async with websockets.connect(uri) as websocket:
                print("[OrchestratorClient] Connected to Unity orchestrator!")
                await websocket.wait_closed()
                print("[OrchestratorClient] Connection closed by Unity orchestrator.")
        except Exception as e:
            print(f"[OrchestratorClient] Connection failed: {e}. Retrying in 2 seconds...")
            await asyncio.sleep(2)

async def main():
    modules = load_modules()
    global my_host, expected_modules, booted_modules, sent_connected, received_connected
    my_host = "pi" if am_i_pi() else "pc"
    print(f"[Orchestrator] Detected self as: {my_host.upper()}")

    expected_modules = {module["name"] for module in modules if module["host"] == my_host}
    booted_modules = set()
    sent_connected = False
    received_connected = False

    orchestrator_port = 9900 if my_host == "pi" else 9901
    global broadcasting_server
    broadcasting_server = BroadcastingServer(port=orchestrator_port)
    broadcasting_server.set_on_message(handle_module_message)
    broadcasting_task = asyncio.create_task(broadcasting_server.run())

    # Start Unity orchestrator reconnect loop in parallel
    unity_connect_task = asyncio.create_task(connect_to_unity_orchestrator())

    await asyncio.sleep(1)

    for module in modules:
        if module["host"] == my_host:
            launch_module(module)

    await asyncio.gather(broadcasting_task, unity_connect_task)

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("[Orchestrator] Shutting down.")
