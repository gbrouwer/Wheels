using WebSocketSharp;
using WebSocketSharp.Server;
using UnityEngine;

public class ServoCommandBehavior : WebSocketBehavior
{
    private WebSocketSessionManager session;
    private ServoCommandServer parentServer;

    public ServoCommandBehavior(ServoCommandServer server)
    {
        parentServer = server;
    }

    protected override void OnOpen()
    {
        session = Sessions;
        parentServer.clientConnected = true;
        Debug.Log("[ServoCommand] ✅ Client connected to Raspberry servo server.");
    }

    public void SendServoCommandJson(string json)
    {
        session.Broadcast(json);
    }

    protected override void OnClose(CloseEventArgs e)
    {
        parentServer.clientConnected = false;
        Debug.LogWarning("[ServoCommand] 🔌 Client disconnected.");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"[ServoCommand] ❌ Error: {e.Message}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"[ServoCommand] ⬅️ Received: {e.Data}");
    }
}
