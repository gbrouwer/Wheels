using UnityEngine;

public class ServoControlTester : MonoBehaviour
{
    private ServoCommandServer servoServer;

    void Start()
    {
        servoServer = GetComponent<ServoCommandServer>();
        if (servoServer == null)
            Debug.LogError("[ServoControlTester] ❌ Missing ServoCommandServer component!");
    }

    void Update()
    {
        // Each key sends complete command with target angles + speed:
        if (Input.GetKeyDown(KeyCode.Q)) SendServoTarget(90, 70, 0.05f);     // slow pan left
        if (Input.GetKeyDown(KeyCode.W)) SendServoTarget(70, 90, 0.02f);    // medium center
        if (Input.GetKeyDown(KeyCode.E)) SendServoTarget(70, 70, 0.01f);   // fast pan right
        if (Input.GetKeyDown(KeyCode.R)) SendServoTarget(80, 80, 0.02f);     // medium tilt down
        if (Input.GetKeyDown(KeyCode.T)) SendServoTarget(90, 90, 0.02f);   // medium tilt up
    }

    private void SendServoTarget(int angle0, int angle1, float rotationSpeed)
    {
        servoServer.SendServoCommand(angle0, angle1, rotationSpeed);
        Debug.Log($"[ServoControlTester] ➡️ Sent: servo0={angle0}, servo1={angle1}, speed={rotationSpeed:F3}s/step");
    }
}
