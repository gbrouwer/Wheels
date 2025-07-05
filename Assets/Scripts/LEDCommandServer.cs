using UnityEngine;
using WebSocketSharp.Server;

public class LEDCommandServer : MonoBehaviour
{
    [Tooltip("Port on which Unity will serve LED commands")]
    public int port = 9020;

    private WebSocketServer server;
    private LEDCommandBehavior ledBehavior;

    [HideInInspector]
    public bool clientConnected = false;

    public void Initialize()
    {
        server = new WebSocketServer(port);
        ledBehavior = new LEDCommandBehavior(this);
        server.AddWebSocketService("/led", () => ledBehavior);
        server.Start();
        Debug.Log($"[LEDCommandServer] ✅ Initialized and listening at ws://localhost:{port}/led");
    }

    void OnDestroy()
    {
        if (server != null && server.IsListening)
        {
            server.Stop();
            Debug.Log("[LEDCommandServer] Server stopped.");
        }
    }

    public void SendLEDCommand(Color[] colors)
    {
        ledBehavior?.SendLEDCommand(colors);
    }
}
