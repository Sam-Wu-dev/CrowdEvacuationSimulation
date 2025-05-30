using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FrameCapture : MonoBehaviour
{
    public Camera captureCamera;
    public string view;

    private int frameCount = 0;
    private string captureDir;

    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string path = Environment.GetEnvironmentVariable("HOME");
        if (!string.IsNullOrEmpty(path))
        {
            path = Path.Combine(path, "Downloads");
        }
        else
        {
            path = Path.Combine(
                Application.dataPath, "ML-Agents", "CGV_Lab", "Projects", "Crowds");
        }

        path = Path.Combine("C:\\Users\\vm3y3\\Downloads");

        captureDir = Path.Combine(
            path, "Captures", currentScene.name, timeStamp, view);
        Directory.CreateDirectory(captureDir);

        Debug.Log($"Frames are saved to {captureDir}");
    }


    void Update()
    {
        CaptureFrame();
    }

    void CaptureFrame()
    {
        RenderTexture renderTexture = captureCamera.targetTexture;
        // Check if RenderTexture exists.
        if (renderTexture == null)
        {
            renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
            captureCamera.targetTexture = renderTexture;
        }

        // Capture frame.
        captureCamera.Render();
        RenderTexture.active = renderTexture;

        Texture2D image = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        // Save image.
        byte[] bytes = image.EncodeToPNG();
        string path = Path.Combine(captureDir, $"frame_{frameCount++}.png");
        File.WriteAllBytes(path, bytes);

        // Clear image.
        Destroy(image);
    }
}