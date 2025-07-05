using UnityEngine;
using System.Collections;

public class LEDWaveTester : MonoBehaviour
{
    [Header("Dependencies")]
    public LEDCommandServer ledServer;

    [Header("Wave Parameters")]
    public Color[] waveColors = new Color[8] {
        Color.red, Color.yellow, Color.green, Color.cyan,
        Color.blue, Color.magenta, Color.white, Color.black
    };
    [Tooltip("Time between frames (seconds)")]
    public float frameDelay = 0.1f;
    [Tooltip("Number of full rotations to run")]
    public int cycles = 5;

    private bool isRunning = false;
    private Coroutine currentAnimation = null;

    void Start()
    {
        if (ledServer == null)
        {
            ledServer = FindObjectOfType<LEDCommandServer>();
            if (ledServer == null)
                Debug.LogError("[LEDWaveTester] ❌ Missing LEDCommandServer — couldn't find one in the scene!");
            else
                Debug.Log("[LEDWaveTester] ℹ️ Found LEDCommandServer automatically.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            CancelCurrentAnimation();
            currentAnimation = StartCoroutine(PlayLEDWave());
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            CancelCurrentAnimation();
            currentAnimation = StartCoroutine(PlayBrightnessWave());
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            CancelCurrentAnimation();
            SetAllLEDsOff();
        }
    }

    void CancelCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
            Debug.Log("[LEDWaveTester] 🔄 Previous animation cancelled.");
        }
    }

    IEnumerator PlayLEDWave()
    {
        isRunning = true;
        int totalFrames = cycles * 8;

        for (int frame = 0; frame < totalFrames; frame++)
        {
            Color[] frameColors = new Color[8];

            // shift waveColors around the ring
            for (int i = 0; i < 8; i++)
            {
                int colorIndex = (frame + i) % waveColors.Length;
                frameColors[i] = waveColors[colorIndex];
            }

            ledServer.SendLEDCommand(frameColors);
            Debug.Log($"[LEDWaveTester] 🚥 Color wave frame {frame + 1}/{totalFrames} sent.");
            yield return new WaitForSeconds(frameDelay);
        }

        isRunning = false;
        currentAnimation = null;
        Debug.Log("[LEDWaveTester] ✅ Color wave animation complete.");
    }
IEnumerator PlayBrightnessWave()
{
    isRunning = true;

    int rotationFramesPerCycle = 8;          // one full ring rotation = 8 frames
    int totalFrames = cycles * rotationFramesPerCycle;

    int brightnessPeriod = 8;                // frames per brightness up+down cycle (faster if < rotation period)

    for (int frame = 0; frame < totalFrames; frame++)
    {
        Color[] frameColors = new Color[8];

        int baseColorIndex = (frame / rotationFramesPerCycle) % waveColors.Length;
        Color baseColor = waveColors[baseColorIndex];

        for (int i = 0; i < 8; i++)
        {
            int ledPhaseOffset = i; // LED1=0, LED2=1,...LED8=7
            int relativeFrame = frame - ledPhaseOffset;

            if (relativeFrame < 0)
            {
                frameColors[i] = Color.black; // LED's wave hasn't arrived yet
                continue;
            }

            // brightness progress: independent faster period (cycles more times per rotation)
            float cycleProgress = (relativeFrame % brightnessPeriod) / (brightnessPeriod / 2f);

            float brightness;
            if (cycleProgress <= 1f)
                brightness = cycleProgress;             // ramp up
            else
                brightness = 2f - cycleProgress;        // ramp down

            brightness = Mathf.Clamp01(brightness);

            // Apply gamma correction
            float correctedBrightness = Mathf.Pow(brightness, 1f / 2.2f);

            frameColors[i] = baseColor * correctedBrightness;
        }

        ledServer.SendLEDCommand(frameColors);
        Debug.Log($"[LEDWaveTester] 🌊 Phase brightness wave frame {frame + 1}/{totalFrames} sent.");
        yield return new WaitForSeconds(frameDelay);
    }

    // Final frame: turn off all LEDs
    SetAllLEDsOff();

    isRunning = false;
    currentAnimation = null;
    Debug.Log("[LEDWaveTester] ✅ Phase brightness wave animation complete, LEDs turned off.");
}


    void SetAllLEDsOff()
    {
        Color[] black = new Color[8];
        for (int i = 0; i < 8; i++) black[i] = Color.black;

        ledServer.SendLEDCommand(black);
        Debug.Log("[LEDWaveTester] 📴 All LEDs set to OFF.");
    }
}
