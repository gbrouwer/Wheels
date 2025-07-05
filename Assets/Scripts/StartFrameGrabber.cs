using System.Diagnostics;
using UnityEngine;

public class StartPythonCapture : MonoBehaviour
{
    [Tooltip("Full path to python.exe")]
    public string pythonPath = @"D:\Robotics\venv\Scripts\python.exe";

    [Tooltip("Full path to process_incoming_frames.py")]
    public string scriptPath = @"D:\Robotics\Wheels\src\windowspc\process_incoming_frames.py";

    private Process pythonProcess;

    void Start()
    {
        StartPythonScript();
    }

    void OnApplicationQuit()
    {
        if (pythonProcess != null && !pythonProcess.HasExited)
        {
            pythonProcess.Kill(); // Optional: clean up on exit
        }
    }

        void StartPythonScript()
        {
            pythonProcess = new Process();
            pythonProcess.StartInfo.FileName = pythonPath;
            pythonProcess.StartInfo.Arguments = $"\"{scriptPath}\"";
            pythonProcess.StartInfo.UseShellExecute = false;
            pythonProcess.StartInfo.CreateNoWindow = true;
            pythonProcess.StartInfo.RedirectStandardOutput = true;
            pythonProcess.StartInfo.RedirectStandardError = true;

            pythonProcess.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.Log($"[PYTHON OUT] {args.Data}");
            };
            pythonProcess.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                    UnityEngine.Debug.LogError($"[PYTHON ERR] {args.Data}");
            };

            pythonProcess.Start();
            pythonProcess.BeginOutputReadLine();
            pythonProcess.BeginErrorReadLine();

            UnityEngine.Debug.Log("✅ Python script started.");
        }
}
