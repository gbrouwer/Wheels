import asyncio
import websockets

class BroadcastingServer:
    def __init__(self, host="0.0.0.0", port=9900):
        self.host = host
        self.port = port
        self.connected_clients = set()
        self.on_message_callback = None

    def set_on_message(self, callback):
        """Set a callback: async def callback(sender_websocket, message: str)"""
        self.on_message_callback = callback

    async def handle_client(self, websocket):
        self.connected_clients.add(websocket)
        print(f"[BroadcastingServer] Client connected: {websocket.remote_address}")
        try:
            async for message in websocket:
                print(f"[BroadcastingServer] Received: {message}")
                if self.on_message_callback:
                    await self.on_message_callback(websocket, message)
        except websockets.ConnectionClosed:
            print(f"[BroadcastingServer] Client disconnected: {websocket.remote_address}")
        finally:
            self.connected_clients.remove(websocket)

    async def broadcast(self, message):
        if self.connected_clients:
            tasks = [client.send(message) for client in self.connected_clients]
            results = await asyncio.gather(*tasks, return_exceptions=True)
            for result in results:
                if isinstance(result, Exception):
                    print(f"[BroadcastingServer] Failed to send message: {result}")

    async def run(self):
        async with websockets.serve(self.handle_client, self.host, self.port):
            print(f"[BroadcastingServer] Server listening on {self.host}:{self.port}")
            await asyncio.Future()  # Run forever
