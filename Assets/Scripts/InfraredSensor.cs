using System;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;
using TMPro;

public class InfraredSensor : MonoBehaviour
{
    public string raspberryPiIP = "192.168.178.129";
    public int port = 6603;
    private WebSocket ws;
    public int latestValue = -1;

    [HideInInspector]
    public bool clientConnected = false;

    public TextMeshProUGUI textElement;

    public event Action<string> OnClientConnected; // ✅ NEW event

    void Start()
    {
        string url = $"ws://{raspberryPiIP}:{port}";
        ws = new WebSocket(url);

        ws.OnMessage += (sender, e) =>
        {
            try
            {
                SensorData data = JsonUtility.FromJson<SensorData>(e.Data);
                latestValue = data.value;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InfraredSensor] Failed to parse message: {e.Data} \n{ex}");
            }
        };

        ws.OnOpen += (sender, e) =>
        {
            clientConnected = true;
            Debug.Log("[InfraredSensor] ✅ Connected to Raspberry infrared server.");
            OnClientConnected?.Invoke("infrared_client"); // ✅ notify orchestrator
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"[InfraredSensor] ❌ WebSocket error: {e.Message}");
        };

        ws.OnClose += (sender, e) =>
        {
            clientConnected = false;
            Debug.LogWarning("[InfraredSensor] 🔌 WebSocket closed.");
        };

        Task.Run(() => ws.Connect());
    }

    void Update()
    {
        if (textElement != null)
        {
            textElement.text = $"Infrared Value: {latestValue} (binary: {Convert.ToString(latestValue, 2).PadLeft(3, '0')})";
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
        public int value;
    }
}
