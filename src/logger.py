import os
import datetime

class Logger:
    def __init__(self, module_name, log_path):
        self.module_name = module_name
        self.log_path = log_path
        os.makedirs(os.path.dirname(self.log_path), exist_ok=True)

    def log(self, message):
        timestamp = datetime.datetime.now().isoformat()
        line = f"[{timestamp}] [{self.module_name}] {message}"
        print(line)
        try:
            with open(self.log_path, "a") as f:
                f.write(line + "\n")
        except Exception as e:
            print(f"[{self.module_name}] Failed to write to log file: {e}")
