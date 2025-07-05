using System;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;
using TMPro;

public class UltrasonicSensor : MonoBehaviour
{
    public string raspberryPiIP = "192.168.178.129"; // Raspberry Pi IP
    public int port = 6604;
    private WebSocket ws;
    public float latestValue = -1f;

    [HideInInspector]
    public bool clientConnected = false; // ✅ orchestrator polling flag

    public TextMeshProUGUI textElement;

    // ✅ NEW: event for orchestrator notification
    public event Action<string> OnClientConnected;

    void Start()
    {
        string url = $"ws://{raspberryPiIP}:{port}";
        ws = new WebSocket(url);

        ws.OnMessage += (sender, e) =>
        {
            try
            {
                SensorData data = JsonUtility.FromJson<SensorData>(e.Data);
                latestValue = data.distance;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UltrasonicSensor] Failed to parse message: {e.Data} \n{ex}");
            }
        };

        ws.OnOpen += (sender, e) =>
        {
            clientConnected = true;
            Debug.Log("[UltrasonicSensor] ✅ Connected to Raspberry ultrasonic server.");
            OnClientConnected?.Invoke("ultrasonic_client"); // ✅ notify orchestrator
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"[UltrasonicSensor] ❌ WebSocket error: {e.Message}");
        };

        ws.OnClose += (sender, e) =>
        {
            clientConnected = false;
            Debug.LogWarning("[UltrasonicSensor] 🔌 WebSocket closed.");
        };

        Task.Run(() => ws.Connect());
    }

    void Update()
    {
        if (textElement != null)
        {
            textElement.text = $"Ultrasonic Distance: {latestValue:F1} cm";
        }
    }

    void OnDestroy()
    {
        if (ws != null && clientConnected)
        {
            ws.Close();
        }
    }

    [Serializable]
    private class SensorData
    {
        public string sensor;
        public float distance;
    }
}
