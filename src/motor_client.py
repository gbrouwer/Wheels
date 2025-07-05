import asyncio
import json
import signal
import sys
from motor import Ordinary_Car
import websockets

class MotorClient:
    def __init__(self, uri="ws://192.168.178.237:9010/motor"):  # ← replace with actual PC IP
        self.uri = uri
        self.motor = Ordinary_Car()
        self.should_run = True

    async def listen_forever(self):
        print(f"[MotorClient] 🚀 Starting. Target server: {self.uri}")
        while self.should_run:
            try:
                print(f"[MotorClient] 🔄 Attempting to connect...")
                async with websockets.connect(self.uri) as websocket:
                    print(f"[MotorClient] ✅ Connected to server.")
                    async for message in websocket:
                        try:
                            data = json.loads(message)
                            d1 = int(data.get("duty1", 0))
                            d2 = int(data.get("duty2", 0))
                            d3 = int(data.get("duty3", 0))
                            d4 = int(data.get("duty4", 0))
                            print(f"[MotorClient] ⬅️ Received command: {data}")
                            self.motor.set_motor_model(d1, d2, d3, d4)
                        except Exception as e:
                            print(f"[MotorClient] ⚠️ Invalid message: {message} — {e}")
            except Exception as e:
                print(f"[MotorClient] ❌ Connection failed: {e}")

            print("[MotorClient] 🔁 Retrying in 2 seconds...")
            await asyncio.sleep(2)

    def cleanup(self):
        print("[MotorClient] 🛑 Shutting down...")
        self.should_run = False
        self.motor.close()
        sys.exit(0)

if __name__ == "__main__":
    client = MotorClient()

    def shutdown(*_):
        client.cleanup()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    try:
        asyncio.run(client.listen_forever())
    except KeyboardInterrupt:
        client.cleanup()
