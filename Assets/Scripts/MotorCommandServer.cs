using UnityEngine;
using WebSocketSharp.Server;

public class MotorCommandServer : MonoBehaviour
{
    public int port = 9010;
    private WebSocketServer server;
    private MotorCommandBehavior motorBehavior;

    [HideInInspector]
    public bool clientConnected = false;

    public void Initialize()
    {
        server = new WebSocketServer(port);

        motorBehavior = new MotorCommandBehavior(this);
        server.AddWebSocketService("/motor", () => motorBehavior);

        server.Start();
        Debug.Log($"[MotorCommandServer] ✅ Initialized and listening on ws://localhost:{port}/motor");
    }

    void OnDestroy()
    {
        if (server != null && server.IsListening)
        {
            server.Stop();
            Debug.Log("[MotorCommandServer] Server stopped.");
        }
    }

    public void SendMotorCommand(int d1, int d2, int d3, int d4)
    {
        motorBehavior?.SendRawDutyCommand(d1, d2, d3, d4);
    }
}
