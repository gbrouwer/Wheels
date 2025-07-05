import asyncio
import json
import signal
import sys
import time
from servo import Servo
import websockets

class ServoClient:
    def __init__(self, uri="ws://192.168.178.237:9040/servo"):  # update IP as needed
        self.uri = uri
        self.servo = Servo()
        self.should_run = True
        self.current_angles = {'0': 90, '1': 90}  # Initialize at midpoint

    async def listen_forever(self):
        print(f"[ServoClient] 🚀 Starting. Target server: {self.uri}")
        while self.should_run:
            try:
                print(f"[ServoClient] 🔄 Attempting to connect...")
                async with websockets.connect(self.uri) as websocket:
                    print(f"[ServoClient] ✅ Connected to server.")
                    async for message in websocket:
                        try:
                            data = json.loads(message)
                            angle0 = int(data.get("servo0", 90))
                            angle1 = int(data.get("servo1", 90))
                            speed = float(data.get("speed", 0.02))  # speed included in every command

                            print(f"[ServoClient] ⬅️ Received command: servo0={angle0}, servo1={angle1}, speed={speed}s/step")

                            self.smooth_move_servo('0', angle0, step_delay=speed)
                            self.smooth_move_servo('1', angle1, step_delay=speed)
                        except Exception as e:
                            print(f"[ServoClient] ⚠️ Invalid message: {message} — {e}")
            except Exception as e:
                print(f"[ServoClient] ❌ Connection failed: {e}")

            print("[ServoClient] 🔁 Retrying in 2 seconds...")
            await asyncio.sleep(2)

    def smooth_move_servo(self, channel, target_angle, step_delay=0.02, step_size=1):
        """Smoothly move a servo to the target angle with incremental steps."""
        current_angle = self.current_angles[channel]
        if current_angle == target_angle:
            print(f"[ServoClient] ✅ Servo {channel} already at {target_angle}°")
            return

        print(f"[ServoClient] 🔄 Moving servo {channel} from {current_angle}° to {target_angle}° at {step_delay:.3f}s/step")

        step = step_size if target_angle > current_angle else -step_size
        for angle in range(current_angle, target_angle, step):
            self.servo.set_servo_pwm(channel, angle)
            time.sleep(step_delay)
        self.servo.set_servo_pwm(channel, target_angle)  # ensure precise final position
        self.current_angles[channel] = target_angle
        print(f"[ServoClient] ✅ Servo {channel} reached {target_angle}°")

    def cleanup(self):
        print("[ServoClient] 🛑 Shutting down...")
        self.should_run = False
        sys.exit(0)

if __name__ == "__main__":
    client = ServoClient()

    def shutdown(*_):
        client.cleanup()

    signal.signal(signal.SIGINT, shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    try:
        asyncio.run(client.listen_forever())
    except KeyboardInterrupt:
        client.cleanup()
