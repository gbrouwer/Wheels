import asyncio
import json
import signal
import sys
import argparse
from motor import Motor
import websockets
from broadcasting_client import BroadcastingClient
from logger import Logger

class MotorClient:
    def __init__(self, host, port):
        self.uri = f"ws://{host}:{port}/motor"
        self.motor = Motor()
        self.should_run = True
        self.broadcasting_client = BroadcastingClient()
        self.logger = Logger("motor_client", "/home/gbrouwer/Wheels/logs/motor_client.log")

    async def listen_forever(self):
        self.logger.log(f"Starting. Target server: {self.uri}")
        await self.send_status_update({"module": "motor_client", "status": "boot_success"})

        while self.should_run:
            try:
                self.logger.log(f"Attempting to connect...")
                async with websockets.connect(self.uri) as websocket:
                    self.logger.log(f"Connected to server.")
                    await self.send_status_update({"module": "motor_client", "status": "connected"})

                    async for message in websocket:
                        try:
                            data = json.loads(message)
                            d1, d2, d3, d4 = data.get("d1"), data.get("d2"), data.get("d3"), data.get("d4")
                            self.logger.log(f"Received command: d1={d1}, d2={d2}, d3={d3}, d4={d4}")
                            self.motor.move(d1, d2, d3, d4)
                        except Exception as e:
                            self.logger.log(f"Invalid message: {message} — {e}")
            except Exception as e:
                self.logger.log(f"Connection failed: {e}")
                await self.send_status_update({"module": "motor_client", "status": "disconnected"})

            self.logger.log("Retrying in 2 seconds...")
            await asyncio.sleep(2)

    async def send_status_update(self, message_dict):
        if self.broadcasting_client.ws:
            try:
                await self.broadcasting_client.send_message(message_dict)
                self.logger.log(f"Sent status update: {message_dict}")
            except Exception as e:
                self.logger.log(f"Failed to send status update: {e}")

    def cleanup(self):
        self.logger.log("Shutting down...")
        self.should_run = False
        self.motor.stop()
        sys.exit(0)

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", required=True)
    parser.add_argument("--port", type=int, required=True)
    args = parser.parse_args()

    client = MotorClient(host=args.host, port=args.port)

    def shutdown(*_):
        client.cleanup()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    try:
        asyncio.run(client.listen_forever())
    except KeyboardInterrupt:
        client.cleanup()
