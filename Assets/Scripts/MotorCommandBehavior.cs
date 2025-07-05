using WebSocketSharp;
using WebSocketSharp.Server;
using UnityEngine;

public class MotorCommandBehavior : WebSocketBehavior
{
    private WebSocketSessionManager session;
    private MotorCommandServer parentServer;

    public MotorCommandBehavior(MotorCommandServer server)
    {
        parentServer = server;
    }

    protected override void OnOpen()
    {
        session = Sessions;
        parentServer.clientConnected = true;
        Debug.Log("[MotorCommand] ✅ Client connected to Raspberry motor server.");
    }

    public void SendRawDutyCommand(int duty1, int duty2, int duty3, int duty4)
    {
        string json = $"{{\"duty1\": {duty1}, \"duty2\": {duty2}, \"duty3\": {duty3}, \"duty4\": {duty4}}}";
        session.Broadcast(json);
        Debug.Log($"[MotorCommand] Sent: {json}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        parentServer.clientConnected = false;
        Debug.LogWarning("[MotorCommand] 🔌 Client disconnected.");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"[MotorCommand] ❌ Error: {e.Message}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"[MotorCommand] ⬅️ Received: {e.Data}");
    }
}
