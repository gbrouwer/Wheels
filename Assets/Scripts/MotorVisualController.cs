using UnityEngine;

public class MotorVisualController : MonoBehaviour
{
    public Transform leftUpperWheel;
    public Transform leftLowerWheel;
    public Transform rightUpperWheel;
    public Transform rightLowerWheel;

    public float rotationSpeedFactor = 0.001f;

    private float duty1, duty2, duty3, duty4;

    public void ApplyMotorCommand(int d1, int d2, int d3, int d4)
    {
        duty1 = d1;
        duty2 = d2;
        duty3 = d3;
        duty4 = d4;
    }

    void Update()
    {
        RotateWheel(leftUpperWheel, duty1);
        RotateWheel(leftLowerWheel, duty2);
        RotateWheel(rightUpperWheel, duty3);
        RotateWheel(rightLowerWheel, duty4);
    }

    void RotateWheel(Transform wheel, float duty)
    {
        if (wheel == null) return;
        float direction = Mathf.Sign(duty);
        float speed = Mathf.Abs(duty) * rotationSpeedFactor;
        wheel.Rotate(Vector3.up * direction * speed * Time.deltaTime * 1000f); // adjust as needed
    }
}
