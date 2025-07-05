import asyncio
import json
import signal
import sys
from infrared import Infrared
from websockets.server import serve
from websockets.exceptions import ConnectionClosed
from broadcasting_client import BroadcastingClient
from logger import Logger

class InfraredSensorServer:
    def __init__(self, port=6602, sensor_name="infrared"):
        self.port = port
        self.sensor_name = sensor_name
        self.infrared = Infrared()
        self.should_run = True
        self.broadcasting_client = BroadcastingClient()
        self.log_path = f"/home/gbrouwer/Wheels/logs/{self.sensor_name}.log"
        self.logger = Logger(self.sensor_name, self.log_path)

    async def stream(self, connection):
        self.logger.log("Unity client connected.")
        await self.send_status_update({
            "module": self.sensor_name,
            "status": "unity_client_connected"
        })
        try:
            while self.should_run:
                value = self.infrared.read_all_infrared()
                payload = json.dumps({"sensor": self.sensor_name, "value": value})
                await connection.send(payload)

                # # Send to broadcasting channel
                # await self.send_status_update({
                #     "module": self.sensor_name,
                #     "reading": {"value": value}
                # })

                await asyncio.sleep(0.1)
        except ConnectionClosed:
            self.logger.log("Unity client disconnected.")
        except Exception as e:
            self.logger.log(f"Error: {e}")

    async def start_server(self):
        self.logger.log(f"Starting WebSocket server on port {self.port}")
        return await serve(self.stream, "0.0.0.0", self.port)

    async def send_status_update(self, message_dict):
        if self.broadcasting_client.ws:
            try:
                await self.broadcasting_client.send_message(message_dict)
                self.logger.log(f"Sent status update: {message_dict}")
            except Exception as e:
                self.logger.log(f"Failed to send message to broadcaster: {e}")

        with open(self.log_path, "a") as log_file:
            log_file.write(json.dumps(message_dict) + "\n")

    async def run(self):
        server = await self.start_server()

        async def send_boot_status():
            await self.send_status_update({"module": self.sensor_name, "status": "boot_success"})

        self.broadcasting_client.on_connect_callback = send_boot_status
        broadcasting_task = asyncio.create_task(self.broadcasting_client.connect_forever())

        self.logger.log("Boot successful. Running server and broadcasting client.")
        await asyncio.gather(server.wait_closed(), broadcasting_task)

    def stop(self):
        self.infrared.close()
        self.logger.log("Server shutdown complete.")
        self.should_run = False
        sys.exit(0)

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, required=True, help="Port number for the sensor server")
    args = parser.parse_args()

    server = InfraredSensorServer(port=args.port)

    def shutdown(*_):
        server.stop()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)
    asyncio.run(server.run())