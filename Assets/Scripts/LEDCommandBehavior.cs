// LEDCommandBehavior.cs
using System.Text;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class LEDCommandBehavior : WebSocketBehavior
{
    private WebSocketSessionManager session;
    private LEDCommandServer parentServer; // NEW: reference to parent

    public LEDCommandBehavior(LEDCommandServer server)
    {
        parentServer = server;
    }

    protected override void OnOpen()
    {
        session = Sessions;
        parentServer.clientConnected = true; // NEW: mark connected
        Debug.Log("[LEDCommand] ✅ Unity actuator client connected to Raspberry LED server.");
    }

    /// <summary>
    /// Broadcasts an array of UnityEngine.Color to all connected WebSocket clients.
    /// </summary>
    public void SendLEDCommand(Color[] colors)
    {
        var sb = new StringBuilder();
        sb.Append("{\"command\":\"setColor\",\"colors\":[");
        for (int i = 0; i < colors.Length; i++)
        {
            var c = colors[i];
            int r = Mathf.RoundToInt(c.r * 255);
            int g = Mathf.RoundToInt(c.g * 255);
            int b = Mathf.RoundToInt(c.b * 255);
            sb.AppendFormat("{{\"r\":{0},\"g\":{1},\"b\":{2}}}", r, g, b);
            if (i < colors.Length - 1) sb.Append(",");
        }
        sb.Append("]}");

        string json = sb.ToString();
        session.Broadcast(json);
        Debug.Log($"[LEDCommand] Broadcast: {json}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"[LEDCommand] ⬅️ Received: {e.Data}");
    }

    protected override void OnError(ErrorEventArgs e)
    {
        Debug.LogError($"[LEDCommand] ❌ Error: {e.Message}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        parentServer.clientConnected = false; // NEW: mark disconnected
        Debug.LogWarning("[LEDCommand] 🔌 Client disconnected from Raspberry LED server.");
    }
}
