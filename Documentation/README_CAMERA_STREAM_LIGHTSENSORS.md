# Wheels: Raspberry Pi Camera Streaming to Unity

This system streams video from a Raspberry Pi camera to a Unity-based application on a PC using RTSP and disk-based frame sharing. It builds on the distributed WebSocket architecture already in place in the Wheels project, adding live vision support.

---

## 📐 Architecture Overview

```
[Raspberry Pi]
├─ mediamtx.service (RTSP Server)
└─ camera-stream.service (libcamera-vid + ffmpeg)
     → Streams RTSP to localhost:8554

[PC]
├─ Unity
│  ├─ StartPythonCapture.cs  → Starts Python
│  └─ LiveFrameViewer.cs     → Loads image from disk
└─ process_incoming_frames.py (RTSP client → writes output_0.jpg ... output_4.jpg)
```

---

## 🧠 Design Summary

| Component              | Purpose                                          |
|------------------------|--------------------------------------------------|
| RTSP via MediaMTX      | Lightweight video stream server                  |
| libcamera-vid + ffmpeg | Low-latency H.264 encoder                        |
| Python Frame Grabber   | Extracts frames and writes to disk               |
| Unity                  | Displays frames in real time via Texture2D       |
| Systemd Services       | Ensure all components start automatically        |

---

## ⚙️ Scripts and Services

### camera-stream.service (Raspberry Pi)

Starts the camera and pipes H.264 video to MediaMTX:

```bash
libcamera-vid -t 0 --width 640 --height 480 --framerate 30 \
  --codec h264 --inline --profile baseline --flush -o - | \
ffmpeg -f h264 -i - -vcodec copy -f rtsp rtsp://localhost:8554/stream
```

---

### mediamtx.service (Raspberry Pi)

Starts the RTSP server using a config at:
```
/home/gbrouwer/Configs/mediamtx.yml
```

---

### process_incoming_frames.py (PC)

- Connects to RTSP stream from Raspberry Pi
- Extracts frames using ffmpeg
- Writes last 5 frames as:
  - `output_0.jpg`
  - `output_1.jpg`
  - ...
  - `output_4.jpg`

---

### StartPythonCapture.cs (Unity)

- Launches `process_incoming_frames.py` using a real `python.exe`
- Can be configured via inspector

---

### LiveFrameViewer.cs (Unity)

- Reads `output_2.jpg` (or similar) at 30 FPS
- Applies frame to a `RawImage`
- Avoids read/write collisions by staying behind the current write index

---

## 🔧 Performance Summary

| Step                    | Time per frame |
|-------------------------|----------------|
| Encode (Pi)             | ~10–20ms       |
| Network (RTSP)          | <5ms           |
| Decode + Save (Python)  | ~10–15ms       |
| Load + Display (Unity)  | ~5–10ms        |
| **Total Latency**       | **~150–250ms** |

---

## 🚀 Services Enabled on Boot

```bash
sudo systemctl enable mediamtx.service
sudo systemctl enable camera-stream.service
```

---

## ✅ Current Resolution

```
640 × 480 px @ 30 fps
JPEG compression on disk (~20–50 KB per frame)
```

---

## 📌 Future Ideas

- Switch to raw .bmp or binary formats
- Directly decode RTSP in Unity (via plugin)
- Use frame timestamps for sensor sync
- Add camera command channel via WebSocket

