using UnityEngine;

public class MotorControlTester : MonoBehaviour
{
    private MotorCommandServer motorServer;
    private MotorVisualController visualController;

    public float moveDuration = 0.25f;
    private bool motorsActive = false;
    private float motorStopTime = 0f;

    void Start()
    {
        motorServer = GetComponent<MotorCommandServer>();
        visualController = GetComponent<MotorVisualController>();

        if (motorServer == null)
            Debug.LogError("[MotorControlTester] ❌ Missing MotorCommandServer!");
        if (visualController == null)
            Debug.LogWarning("[MotorControlTester] ℹ️ No MotorVisualController assigned.");
    }

    void ActivateMotors(int d1, int d2, int d3, int d4)
    {
        if (motorsActive)
        {
            Debug.Log("[MotorControlTester] 🔁 Ignored — motors already active");
            return;
        }

        motorsActive = true;
        motorStopTime = Time.time + moveDuration;

        motorServer?.SendMotorCommand(d1, d2, d3, d4);
        visualController?.ApplyMotorCommand(d1, d2, d3, d4);
        Debug.Log($"[MotorControlTester] 🚗 Sent movement command: ({d1}, {d2}, {d3}, {d4})");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) ActivateMotors(1000, 1000, 1000, 1000);
        if (Input.GetKeyDown(KeyCode.S)) ActivateMotors(-1000, -1000, -1000, -1000);
        if (Input.GetKeyDown(KeyCode.A)) ActivateMotors(-1500, -1500, 2000, 2000);
        if (Input.GetKeyDown(KeyCode.D)) ActivateMotors(2000, 2000, -1500, -1500);

        // Send stop when time expires
        if (motorsActive && Time.time >= motorStopTime)
        {
            motorServer?.SendMotorCommand(0, 0, 0, 0);
            visualController?.ApplyMotorCommand(0, 0, 0, 0);
            motorsActive = false;
            Debug.Log("[MotorControlTester] ⛔ Motors stopped.");
        }
    }
}
