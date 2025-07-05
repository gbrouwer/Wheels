using UnityEngine;

public class SpeakerControlTester : MonoBehaviour
{
    private SpeakerCommandServer speakerServer;

    void Start()
    {
        speakerServer = GetComponent<SpeakerCommandServer>();
        if (speakerServer == null)
            Debug.LogError("[SpeakerControlTester] Missing SpeakerCommandServer component!");
    }

    void Update()
    {
        // For demonstration, use the same keys as before,
        // but all send a random number between 1 and 28 as string.
        if (Input.GetKeyDown(KeyCode.Alpha1)) SendRandomSoundCode();
        if (Input.GetKeyDown(KeyCode.Alpha2)) SendRandomSoundCode();
        if (Input.GetKeyDown(KeyCode.Alpha0)) SendRandomSoundCode();
    }

    private void SendRandomSoundCode()
    {
        int randomDigit = Random.Range(1, 29); // Unity's Random.Range upper bound is exclusive
        string soundCode = randomDigit.ToString();
        speakerServer.SendSoundCommand(soundCode);
        Debug.Log($"[SpeakerControlTester] Sent random sound command: {soundCode}");
    }
}
