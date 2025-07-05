using UnityEngine;
using WebSocketSharp.Server;

public class SpeakerCommandServer : MonoBehaviour
{
    [Tooltip("Port on which Unity will serve speaker commands")]
    public int port = 9040;

    private WebSocketServer server;
    private SpeakerCommandBehavior speakerBehavior;

    [HideInInspector]
    public bool clientConnected = false;

    public void Initialize()
    {
        server = new WebSocketServer(port);
        speakerBehavior = new SpeakerCommandBehavior(this);
        server.AddWebSocketService("/speaker", () => speakerBehavior);
        server.Start();
        Debug.Log($"[SpeakerCommandServer] ✅ Initialized and listening at ws://localhost:{port}/speaker");
    }

    void OnDestroy()
    {
        if (server != null && server.IsListening)
        {
            server.Stop();
            Debug.Log("[SpeakerCommandServer] Server stopped.");
        }
    }

    public void SendSoundCommand(string soundCode)
    {
        speakerBehavior?.SendSoundCommand(soundCode);
    }
}
