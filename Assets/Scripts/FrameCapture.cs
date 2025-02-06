using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class FrameCapture : MonoBehaviour
{
    public int captureFrequency = 10; // Frames per second
    public string savePath = "CapturedFrames";
    public string trialName = "Trial01";
    private Camera captureCamera;
    private float nextCaptureTime;
    
    void Start()
    {
        savePath = savePath + "/" + trialName;
        captureCamera = GetComponent<Camera>();
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
        nextCaptureTime = Time.time;
    }

    void Update()
    {
        if (Time.time >= nextCaptureTime)
        {
            StartCoroutine(CaptureFrame());
            nextCaptureTime = Time.time + (1.0f / captureFrequency);
        }
    }

    IEnumerator CaptureFrame()
    {
        yield return new WaitForEndOfFrame();
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        captureCamera.targetTexture = renderTexture;
        Texture2D texture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        
        captureCamera.Render();
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        texture.Apply();
        
        byte[] bytes = texture.EncodeToPNG();
        string filePath = Path.Combine(savePath, $"frame_{Time.frameCount}.png");
        File.WriteAllBytes(filePath, bytes);
        
        Debug.Log("Frame captured: " + filePath);
        
        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);
        Destroy(texture);
    }
}
