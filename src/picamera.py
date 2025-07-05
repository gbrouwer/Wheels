import asyncio
import json
import signal
import sys
import base64
from picamera2 import Picamera2
import cv2
from websockets.server import serve
from websockets.exceptions import ConnectionClosed
from broadcasting_client import BroadcastingClient
from logger import Logger

class PicameraServer:
    def __init__(self, port=6604, sensor_name="picam"):
        self.port = port
        self.sensor_name = sensor_name
        self.picam2 = Picamera2()
        self.picam2.configure(self.picam2.create_preview_configuration(main={"size": (640, 480)}))
        self.picam2.start()
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
                frame = self.picam2.capture_array()
                frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                # mean_rgb = frame.mean(axis=(0,1)).tolist()

                _, jpeg = cv2.imencode('.jpg', frame, [int(cv2.IMWRITE_JPEG_QUALITY), 70])
                b64_image = base64.b64encode(jpeg.tobytes()).decode('utf-8')

                await connection.send(json.dumps({"sensor": self.sensor_name, "image": b64_image}))

                # # Send mean RGB to broadcasting channel (not the full image)
                # await self.send_status_update({
                #     "module": self.sensor_name,
                #     "reading": {"mean_rgb": mean_rgb}
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
        self.picam2.stop()
        self.logger.log("Server shutdown complete.")
        self.should_run = False
        sys.exit(0)

if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, required=True, help="Port number for the sensor server")
    args = parser.parse_args()

    server = PicameraServer(port=args.port)

    def shutdown(*_):
        server.stop()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)
    asyncio.run(server.run())
