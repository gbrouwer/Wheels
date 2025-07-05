using UnityEngine;
using WebSocketSharp;
using System.Collections;

public class UnityOrchestrator : MonoBehaviour
{
    [Header("Unity Sensor Clients")]
    public UltrasonicSensor ultrasonicClient;
    public InfraredSensor infraredClient;
    public LightSensor lightLeftClient;
    public LightSensor lightRightClient;
    public PiCameraSensor piCameraClient;

    [Header("Unity Actuator Servers")]
    public MotorCommandServer motorServer;
    public LEDCommandServer ledServer;
    public ServoCommandServer servoServer;
    public SpeakerCommandServer speakerServer;

    [Header("Pi Orchestrator Connection")]
    public string raspberryIP = "192.168.178.129";
    public int raspberryPort = 9900;

    private WebSocket ws;

    void Start()
    {
        ultrasonicClient.OnClientConnected += OnSensorClientConnected;
        infraredClient.OnClientConnected += OnSensorClientConnected;
        lightLeftClient.OnClientConnected += OnSensorClientConnected;
        lightRightClient.OnClientConnected += OnSensorClientConnected;
        piCameraClient.OnClientConnected += OnSensorClientConnected;

        StartCoroutine(ConnectToPiOrchestrator());
    }

    private IEnumerator ConnectToPiOrchestrator()
    {
        string uri = $"ws://{raspberryIP}:{raspberryPort}";
        while (true)
        {
            Debug.Log($"[UnityOrchestratorClient] 🔄 Connecting to Raspberry orchestrator at {uri}...");
            ws = new WebSocket(uri);

            ws.OnOpen += (s, e) =>
            {
                Debug.Log("[UnityOrchestratorClient] ✅ Connected to Raspberry orchestrator!");
            };

            ws.OnMessage += (s, e) =>
            {
                Debug.Log($"[UnityOrchestratorClient] ⬅️ Received from Raspberry orchestrator: {e.Data}");
                try
                {
                    OrchestratorMessage msg = JsonUtility.FromJson<OrchestratorMessage>(e.Data);
                    if (msg.status == "actuator_clients_started")
                    {
                        Debug.Log("[UnityOrchestrator] 🚦 Raspberry actuator clients started. Starting Unity actuator servers!");
                        StartUnityActuatorServers();
                    }
                    else if (msg.status == "all_actuators_connected")
                    {
                        Debug.Log("[UnityOrchestrator] 🎉 All actuators fully connected. System ready for operation!");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[UnityOrchestrator] ⚠️ Failed to parse orchestrator message: {ex.Message}");
                }
            };

            ws.OnError += (s, e) =>
            {
                Debug.LogError($"[UnityOrchestratorClient] ❌ Error: {e.Message}");
            };

            ws.OnClose += (s, e) =>
            {
                Debug.LogWarning("[UnityOrchestratorClient] 🔌 Connection closed.");
            };

            ws.Connect();

            while (ws.IsAlive)
            {
                yield return null;
            }

            Debug.LogWarning("[UnityOrchestratorClient] 🔁 Disconnected. Retrying in 2 seconds...");
            yield return new WaitForSeconds(2f);
        }
    }

    private void OnSensorClientConnected(string moduleName)
    {
        Debug.Log($"[UnityOrchestrator] ✅ Sensor client connected: {moduleName}");

        if (ws != null && ws.IsAlive)
        {
            string json = $"{{\"module\":\"{moduleName}\",\"status\":\"connected\"}}";
            ws.Send(json);
            Debug.Log($"[UnityOrchestrator] 📡 Sent client connection message: {json}");
        }
        else
        {
            Debug.LogWarning("[UnityOrchestrator] ⚠️ Orchestrator WebSocket not yet connected — message will not be sent!");
        }
    }

    private void StartUnityActuatorServers()
    {
        motorServer.Initialize();
        ledServer.Initialize();
        servoServer.Initialize();
        speakerServer.Initialize();
        Debug.Log("[UnityOrchestrator] ✅ Unity actuator servers initialized.");
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
            Debug.Log("[UnityOrchestratorClient] 🔌 Connection closed on destroy.");
        }
    }

    [System.Serializable]
    private class OrchestratorMessage
    {
        public string orchestrator;
        public string status;
    }
}
