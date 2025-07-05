using WebSocketSharp;
using WebSocketSharp.Server;
using UnityEngine;

public class SpeakerCommandBehavior : WebSocketBehavior
{
    private WebSocketSessionManager session;
    private SpeakerCommandServer parentServer;

    public SpeakerCommandBehavior(SpeakerCommandServer server)
    {
        parentServer = server;
    }

    protected override void OnOpen()
    {
        session = Sessions;
        parentServer.clientConnected = true;
        Debug.Log("[SpeakerCommand] ✅ Client connected to Raspberry speaker server.");
    }

    public void SendSoundCommand(string soundCode)
    {
        string json = $"{{\"sound\": \"{soundCode}\"}}";
        session.Broadcast(json);
        Debug.Log($"[SpeakerCommand] Sent: {json}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        parentServer.clientConnected = false;
        Debug.LogWarning("[SpeakerCommand] 🔌 Client disconnected.");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"[SpeakerCommand] ❌ Error: {e.Message}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"[SpeakerCommand] ⬅️ Received: {e.Data}");
    }
}
