import subprocess
import signal
import sys
import os
import time
import tty
import termios
import select
import threading

def start_script(script_name):
    print(f"[agent.py] 🔄 Launching script: {script_name}")
    return subprocess.Popen(
        [sys.executable, script_name],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        preexec_fn=os.setsid,
        text=True  # so we can print strings instead of bytes
    )

def esc_pressed():
    dr, _, _ = select.select([sys.stdin], [], [], 0)
    if dr:
        c = sys.stdin.read(1)
        return c == '\x1b'
    return False

def stream_output(stream, name, is_error=False):
    for line in iter(stream.readline, ''):
        prefix = "⚠️" if is_error else "⬅️"
        print(f"[{name}] {prefix} {line.strip()}")

def attach_stream_listeners(proc, name):
    threading.Thread(target=stream_output, args=(proc.stdout, name), daemon=True).start()
    threading.Thread(target=stream_output, args=(proc.stderr, name, True), daemon=True).start()

def main():
    print("[agent.py] 🚀 Starting all sensor, motor and LED modules... (Press ESC to terminate)")

    # switch terminal to raw mode to capture ESC
    stdin_fd = sys.stdin.fileno()
    old_settings = termios.tcgetattr(stdin_fd)
    tty.setcbreak(stdin_fd)

    scripts = [
        ("light_sensor_left.py", "LeftSensor"),
        ("light_sensor_right.py", "RightSensor"),
        ("motor_client.py",     "MotorClient"),
        ("led_client.py",       "LEDClient")   # ← newly added
    ]

    processes = []
    try:
        for script, name in scripts:
            proc = start_script(script)
            attach_stream_listeners(proc, name)
            processes.append((proc, name))

        while True:
            if esc_pressed():
                print("\n[agent.py] 🛑 ESC pressed. Terminating all modules...")
                break
            time.sleep(0.1)

    finally:
        # restore terminal
        termios.tcsetattr(stdin_fd, termios.TCSADRAIN, old_settings)
        for proc, name in processes:
            if proc.poll() is None:
                try:
                    print(f"[agent.py] ✋ Terminating {name}...")
                    os.killpg(os.getpgid(proc.pid), signal.SIGTERM)
                except Exception as e:
                    print(f"[agent.py] ⚠️ Error terminating {name}: {e}")
        print("[agent.py] ✅ All subprocesses terminated. Exiting.")

if __name__ == "__main__":
    main()
