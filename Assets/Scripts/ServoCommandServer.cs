using UnityEngine;
using WebSocketSharp.Server;
using System.Globalization;

public class ServoCommandServer : MonoBehaviour
{
    public int port = 9030;
    private WebSocketServer server;
    private ServoCommandBehavior servoBehavior;

    [HideInInspector]
    public bool clientConnected = false;

    public void Initialize()
    {
        server = new WebSocketServer(port);

        servoBehavior = new ServoCommandBehavior(this);
        server.AddWebSocketService("/servo", () => servoBehavior);

        server.Start();
        Debug.Log($"[ServoCommandServer] ✅ Initialized and listening on ws://localhost:{port}/servo");
    }

    void OnDestroy()
    {
        if (server != null && server.IsListening)
        {
            server.Stop();
            Debug.Log("[ServoCommandServer] Server stopped.");
        }
    }

    public void SendServoCommand(int angle0, int angle1, float speed)
    {
        string speedFormatted = speed.ToString("F4", CultureInfo.InvariantCulture);
        string json = $"{{\"servo0\": {angle0}, \"servo1\": {angle1}, \"speed\": {speedFormatted}}}";
        servoBehavior?.SendServoCommandJson(json);
        Debug.Log($"[ServoCommand] Sent: {json}");
    }
}
