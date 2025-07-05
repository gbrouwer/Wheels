using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using WebSocketSharp;

public class PiCameraSensor : MonoBehaviour
{
    [Header("UI & Connection")]
    public RawImage targetImage;
    public string raspberryPiIP = "192.168.178.129";
    public int port = 6600;

    private Texture2D tex;
    private WebSocket ws;
    private readonly object frameLock = new object();
    private byte[] latestImageBytes;
    private bool newFrameReady = false;

    [HideInInspector]
    public bool clientConnected = false;

    public event Action<string> OnClientConnected; // ✅ NEW event

    void Start()
    {
        tex = new Texture2D(2, 2);
        targetImage.texture = tex;

        ws = new WebSocket($"ws://{raspberryPiIP}:{port}");

        ws.OnMessage += (sender, e) =>
        {
            try
            {
                var json = JsonUtility.FromJson<FrameMessage>(e.Data);
                byte[] imageData = Convert.FromBase64String(json.image);

                lock (frameLock)
                {
                    latestImageBytes = imageData;
                    newFrameReady = true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PiCameraSensor] Failed to parse frame: {ex}");
            }
        };

        ws.OnOpen += (sender, e) =>
        {
            clientConnected = true;
            Debug.Log("[PiCameraSensor] ✅ Connected to Raspberry Pi camera server.");
            OnClientConnected?.Invoke("picamera_client"); // ✅ notify orchestrator
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"[PiCameraSensor] ❌ WebSocket error: {e.Message}");
        };

        ws.OnClose += (sender, e) =>
        {
            clientConnected = false;
            Debug.LogWarning("[PiCameraSensor] 🔌 WebSocket closed.");
        };

        Task.Run(() => ws.Connect());
    }

    void Update()
    {
        if (newFrameReady)
        {
            byte[] imageToLoad;
            lock (frameLock)
            {
                imageToLoad = latestImageBytes;
                newFrameReady = false;
            }

            try
            {
                tex.LoadImage(imageToLoad);
                tex.Apply();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PiCameraSensor] Failed to load image: {ex.Message}");
            }
        }
    }

    void OnDestroy()
    {
        if (ws != null && clientConnected)
            ws.Close();
    }

    [Serializable]
    private class FrameMessage
    {
        public string sensor;
        public string image;
    }
}
