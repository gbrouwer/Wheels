import asyncio
import json
import signal
import sys
import argparse
from servo import Servo
import websockets
from broadcasting_client import BroadcastingClient
from logger import Logger

class ServoClient:
    def __init__(self, host, port):
        self.uri = f"ws://{host}:{port}/servo"
        self.servo = Servo()
        self.should_run = True
        self.broadcasting_client = BroadcastingClient()
        self.logger = Logger("servo_client", "/home/gbrouwer/Wheels/logs/servo_client.log")

    async def listen_forever(self):
        self.logger.log(f"Starting. Target server: {self.uri}")
        await self.send_status_update({"module": "servo_client", "status": "boot_success"})

        while self.should_run:
            try:
                self.logger.log(f"Attempting to connect...")
                async with websockets.connect(self.uri) as websocket:
                    self.logger.log(f"Connected to server.")
                    await self.send_status_update({"module": "servo_client", "status": "connected"})

                    async for message in websocket:
                        try:
                            data = json.loads(message)
                            s0, s1, speed = data.get("servo0"), data.get("servo1"), data.get("speed")
                            self.logger.log(f"Received command: servo0={s0}, servo1={s1}, speed={speed}")
                            self.servo.move(s0, s1, speed)
                        except Exception as e:
                            self.logger.log(f"Invalid message: {message} — {e}")
            except Exception as e:
                self.logger.log(f"Connection failed: {e}")
                await self.send_status_update({"module": "servo_client", "status": "disconnected"})

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
        self.servo.stop()
        sys.exit(0)

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", required=True)
    parser.add_argument("--port", type=int, required=True)
    args = parser.parse_args()

    client = ServoClient(host=args.host, port=args.port)

    def shutdown(*_):
        client.cleanup()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    try:
        asyncio.run(client.listen_forever())
    except KeyboardInterrupt:
        client.cleanup()
