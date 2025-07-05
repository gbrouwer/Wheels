import asyncio
import json
import signal
import sys
from led import Led
import websockets

class LEDClient:
    def __init__(self, uri="ws://192.168.178.237:9020/led"):  # ← replace with your actual Unity IP and LED port
        self.uri = uri
        self.led = Led()
        self.should_run = True

    async def listen_forever(self):
        print(f"[LEDClient] 🚀 Starting. Target server: {self.uri}")
        while self.should_run:
            try:
                print(f"[LEDClient] 🔄 Attempting to connect...")
                async with websockets.connect(self.uri) as websocket:
                    print(f"[LEDClient] ✅ Connected to server.")
                    async for message in websocket:
                        try:
                            data = json.loads(message)
                            command = data.get("command", "")
                            colors = data.get("colors", [])

                            print(f"[LEDClient] ⬅️ Received command: {command} with colors: {colors}")

                            if command == "setColor" and len(colors) == 8:
                                for i, color in enumerate(colors):
                                    r, g, b = color.get("r", 0), color.get("g", 0), color.get("b", 0)
                                    self.led.strip.set_led_rgb_data(i, [r, g, b])
                                self.led.strip.show()
                            elif command == "turnOff":
                                self.led.strip.set_all_led_color(0, 0, 0)
                                self.led.strip.show()

                        except Exception as e:
                            print(f"[LEDClient] ⚠️ Invalid message: {message} — {e}")
            except Exception as e:
                print(f"[LEDClient] ❌ Connection failed: {e}")

            print("[LEDClient] 🔁 Retrying in 2 seconds...")
            await asyncio.sleep(2)

    def cleanup(self):
        print("[LEDClient] 🛑 Shutting down...")
        self.should_run = False
        self.led.strip.set_all_led_color(0, 0, 0)
        self.led.strip.show()
        sys.exit(0)

if __name__ == "__main__":
    client = LEDClient()

    def shutdown(*_):
        client.cleanup()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    try:
        asyncio.run(client.listen_forever())
    except KeyboardInterrupt:
        client.cleanup()
