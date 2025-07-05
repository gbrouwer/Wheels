import asyncio
import websockets
import json

class BroadcastingClient:
    def __init__(self, uri="ws://192.168.178.137:9901"):
        self.uri = uri
        self.ws = None
        self.should_run = True
        self.on_connect_callback = None  # new hook

    async def connect_forever(self):
        while self.should_run:
            try:
                self.ws = await websockets.connect(self.uri)
                print("[BroadcastingClient] Connected to orchestrator broadcasting server.")

                # call your hook after a successful connection
                if self.on_connect_callback:
                    await self.on_connect_callback()

                await self.listen()  # blocks until connection closes
            except Exception as e:
                print(f"[BroadcastingClient] Connection failed: {e}")
            print("[BroadcastingClient] Reconnecting in 2 seconds...")
            await asyncio.sleep(2)

    async def listen(self):
        try:
            async for message in self.ws:
                print(f"[BroadcastingClient] Received message from orchestrator: {message}")
        except websockets.ConnectionClosed:
            print("[BroadcastingClient] Connection closed.")

    async def send_message(self, message_dict):
        if self.ws:
            try:
                await self.ws.send(json.dumps(message_dict))
            except Exception as e:
                print(f"[BroadcastingClient] Failed to send message: {e}")

    def stop(self):
        self.should_run = False
