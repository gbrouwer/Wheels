using System;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;
using TMPro;

public class LightSensor : MonoBehaviour
{
    public string raspberryPiIP = "192.168.178.129";
    public int port = 6601; // left or right light sensor port
    private WebSocket ws;
    public float latestValue = -1f;

    [HideInInspector]
    public bool clientConnected = false;

    public TextMeshProUGUI textElement;

    public event Action<string> OnClientConnected; // ✅ NEW event

    void Start()
    {
        ws = new WebSocket($"ws://{raspberryPiIP}:{port}");

        ws.OnMessage += (sender, e) =>
        {
            try
            {
                SensorData data = JsonUtility.FromJson<SensorData>(e.Data);
                latestValue = data.value;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LightSensor] Failed to parse message: {e.Data} \n{ex}");
            }
        };

        ws.OnOpen += (sender, e) =>
        {
            clientConnected = true;
            Debug.Log("[LightSensor] ✅ Connected to Raspberry light sensor server.");
            string moduleName = port == 6601 ? "light_left_client" : "light_right_client";
            OnClientConnected?.Invoke(moduleName); // ✅ notify orchestrator
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"[LightSensor] ❌ WebSocket error: {e.Message}");
        };

        ws.OnClose += (sender, e) =>
        {
            clientConnected = false;
            Debug.LogWarning("[LightSensor] 🔌 WebSocket closed.");
        };

        Task.Run(() => ws.Connect());
    }

    void Update()
    {
        if (textElement != null)
        {
            textElement.text = $"Light Value: {latestValue:F1}";
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
        public float value;
    }
}
