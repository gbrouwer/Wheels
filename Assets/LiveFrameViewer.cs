using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class LiveFrameViewer : MonoBehaviour
{
    public RawImage targetImage;
    public string frameFolder = @"D:\Robotics\Wheels\src\windowspc\frames";
    public string framePrefix = "output";
    public int frameIndexToRead = 2; // Choose a safe index like 2 if N = 5
    public float refreshRate = 0.1f; // Seconds between reads

    private Texture2D tex;
    private float timer;

    void Start()
    {
        tex = new Texture2D(2, 2); // Placeholder size, will resize on load
        targetImage.texture = tex;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= refreshRate)
        {
            timer = 0f;
            string path = Path.Combine(frameFolder, $"{framePrefix}_{frameIndexToRead}.jpg");
            if (File.Exists(path))
            {
                byte[] imageBytes = File.ReadAllBytes(path);
                tex.LoadImage(imageBytes); // Automatically resizes
                tex.Apply();
            }
            else
            {
                Debug.Log(path);
            }
        }
    }
}
