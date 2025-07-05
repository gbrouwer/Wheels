using UnityEngine;
using System.Collections;

public class BootSequence : MonoBehaviour
{
    [Header("Dependencies")]
    public LEDCommandServer ledServer;
    public MotorCommandServer motorServer;
    public ServoCommandServer servoServer;
    public SpeakerCommandServer speakerServer;

    public float ledWaveSpeed = 0.25f;
    public int ledWaveCycles = 2;
    public float motorMoveDuration = 0.3f;
    public float servoSpeed = 0.02f;

    void Start()
    {
        StartCoroutine(RunBootSequence());
    }

    private IEnumerator RunBootSequence()
    {

        yield return WaitForClient(speakerServer, "Speaker");
        SendRandomSound();


        // yield return WaitForClient(ledServer, "LED");
        // yield return PlayLEDWave();



        // yield return WaitForClient(motorServer, "Motor");
        // yield return MoveMotors();

        // yield return WaitForClient(servoServer, "Servo");
        // SendServoTarget(80, 80, servoSpeed);

    }

    private IEnumerator WaitForClient(object server, string name)
    {
        Debug.Log($"[BootSequence] ⏳ Waiting for {name} actuator client to connect...");
        bool connected = false;
        while (!connected)
        {
            if (server is LEDCommandServer led && led.clientConnected) connected = true;
            if (server is MotorCommandServer motor && motor.clientConnected) connected = true;
            if (server is ServoCommandServer servo && servo.clientConnected) connected = true;
            if (server is SpeakerCommandServer speaker && speaker.clientConnected) connected = true;
            yield return null;
        }
        Debug.Log($"[BootSequence] ✅ {name} actuator client connected.");
    }

    private IEnumerator PlayLEDWave()
    {
        int ledCount = 8;
        Color off = Color.black;
        Color waveColor = Color.white;

        for (int cycle = 0; cycle < ledWaveCycles; cycle++)
        {
            for (int i = 0; i < ledCount; i++)
            {
                Color[] colors = new Color[ledCount];
                for (int j = 0; j < ledCount; j++)
                {
                    colors[j] = (j == i) ? waveColor : off;
                }
                ledServer.SendLEDCommand(colors);
                yield return new WaitForSeconds(ledWaveSpeed);
            }
        }

        ledServer.SendLEDCommand(new Color[ledCount]);
        Debug.Log("[BootSequence] 🏁 LED boot wave animation complete.");
    }

    private IEnumerator MoveMotors()
    {
        motorServer.SendMotorCommand(1000, 1000, 1000, 1000);
        Debug.Log("[BootSequence] 🚗 Sent forward motor command.");
        yield return new WaitForSeconds(motorMoveDuration);

        motorServer.SendMotorCommand(-1000, -1000, -1000, -1000);
        Debug.Log("[BootSequence] 🚗 Sent backward motor command.");
        yield return new WaitForSeconds(motorMoveDuration);

        motorServer.SendMotorCommand(0, 0, 0, 0);
        Debug.Log("[BootSequence] ⛔ Motors stopped.");
    }

    private void SendServoTarget(int angle0, int angle1, float speed)
    {
        servoServer.SendServoCommand(angle0, angle1, speed);
        Debug.Log($"[BootSequence] ➡️ Sent servo target: servo0={angle0}, servo1={angle1}, speed={speed:F3}s/step");
    }

    private void SendRandomSound()
    {
        int sound = Random.Range(1, 29);
        speakerServer.SendSoundCommand(sound.ToString());
        Debug.Log($"[BootSequence] 🔊 Sent random sound command: {sound}");
    }
}
