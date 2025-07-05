import asyncio
import json
import signal
import sys
import websockets
from ultrasonic import Ultrasonic  # <-- your real sensor driver
from broadcasting_client import BroadcastingClient
from logger import Logger

class UltrasonicSensorServer:
    def __init__(self, port=6603, sensor_name="ultrasonic_sensor"):
        self.port = port
        self.sensor_name = sensor_name
        self.should_run = True
        self.broadcasting_client = BroadcastingClient()
        self.log_path = f"/home/gbrouwer/Wheels/logs/{self.sensor_name}.log"
        self.logger = Logger(self.sensor_name, self.log_path)
        self.ultrasonic = Ultrasonic(trigger_pin=27, echo_pin=22)  # Initialize your ultrasonic hardware

    async def handle_client(self, websocket):
        self.logger.log("Unity client connected.")

        # Send status update: Unity client connected
        await self.send_status_update({
            "module": self.sensor_name,
            "status": "unity_client_connected"
        })

        try:
            while self.should_run:
                distance = self.ultrasonic.get_distance()  # Read real measurement
                if distance is None:
                    self.logger.log("Warning: Ultrasonic reading timed out or failed.")
                    continue

                payload = json.dumps({
                    "sensor": self.sensor_name,
                    "distance": distance
                })

                # Send to Unity client
                await websocket.send(payload)

                # # Also send reading to orchestrator via broadcasting channel
                # await self.send_status_update({
                #     "module": self.sensor_name,
                #     "reading": {"distance": distance}
                # })

                await asyncio.sleep(1)
        except websockets.ConnectionClosed:
            self.logger.log("Unity client disconnected.")
            await self.send_status_update({
                "module": self.sensor_name,
                "status": "unity_client_disconnected"
            })

    async def start_server(self):
        self.logger.log(f"Starting WebSocket server on port {self.port}")
        return await websockets.serve(self.handle_client, "0.0.0.0", self.port)

    async def send_status_update(self, message_dict):
        if self.broadcasting_client.ws:
            try:
                await self.broadcasting_client.send_message(message_dict)
                self.logger.log(f"Sent status update: {message_dict}")
            except Exception as e:
                self.logger.log(f"Failed to send message to broadcaster: {e}")

        try:
            with open(self.log_path, "a") as log_file:
                log_file.write(json.dumps(message_dict) + "\n")
        except Exception as e:
            self.logger.log(f"Failed to write status log: {e}")

    async def run(self):
        server = await self.start_server()

        async def send_boot_status():
            await self.send_status_update({"module": self.sensor_name, "status": "boot_success"})

        self.broadcasting_client.on_connect_callback = send_boot_status

        broadcasting_task = asyncio.create_task(self.broadcasting_client.connect_forever())

        self.logger.log("Boot successful. Running server and broadcasting client.")

        await asyncio.gather(
            server.wait_closed(),
            broadcasting_task,
        )

    def stop(self):
        self.logger.log("Shutting down...")
        self.ultrasonic.close()  # Gracefully release GPIO resources
        self.should_run = False
        sys.exit(0)

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, required=True, help="Port number for the sensor server")
    args = parser.parse_args()

    server = UltrasonicSensorServer(port=args.port)

    def shutdown(*_):
        server.stop()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)
    asyncio.run(server.run())
