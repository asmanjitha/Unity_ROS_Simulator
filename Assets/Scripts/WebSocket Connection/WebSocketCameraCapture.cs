using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using UnityEngine.UI;
using System.Numerics;
using Unity.VisualScripting;

public class WebSocketCameraCapture : MonoBehaviour
{
    public Camera captureCamera;  
    public RawImage displayImage; 
    public GameObject boundingBoxPrefab; 

    private ClientWebSocket ws;
    private bool isConnected = false;
    public List<GameObject> activeBoundingBoxes = new List<GameObject>();

    public GameObject canvas;

    public bool autoIdentifyHumans = true;
    private bool findHumanInView = false;

    async Task Start()
    {
        await ConnectToServer();
        StartCoroutine(CaptureAndSend());
        _ = ReceiveMessages();  // Start listening for responses
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            
            findHumanInView = true;
        }
    }

    async Task ConnectToServer()
    {
        ws = new ClientWebSocket();
        Uri serverUri = new Uri("ws://localhost:4200");

        try
        {
            await ws.ConnectAsync(serverUri, CancellationToken.None);
            isConnected = true;
            Debug.Log("✅ Connected to WebSocket server!");
        }
        catch (Exception e)
        {
            Debug.LogError("❌ WebSocket connection error: " + e.Message);
        }
    }

    IEnumerator CaptureAndSend()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.5f);

            if (isConnected)
            {
                Texture2D screenshot = CaptureCameraView();
                byte[] imageBytes = screenshot.EncodeToPNG();
                Destroy(screenshot);
                SendImageToServer(imageBytes);
            }
        }
    }

    Texture2D CaptureCameraView()
    {
        RenderTexture rt = new RenderTexture(1920, 1080, 16);
        captureCamera.targetTexture = rt;
        captureCamera.Render();

        RenderTexture.active = rt;
        Texture2D screenshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        screenshot.Apply();

        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        return screenshot;
    }

    async void SendImageToServer(byte[] imageBytes)
    {
        if (ws.State == WebSocketState.Open)
        {
            await ws.SendAsync(new ArraySegment<byte>(imageBytes), WebSocketMessageType.Binary, true, CancellationToken.None);
            // Debug.Log($"📤 Sent {imageBytes.Length} bytes to server.");
        }
    }

    async Task ReceiveMessages()
    {
        byte[] buffer = new byte[1024 * 1024]; // 1MB buffer

        while (ws.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            
            if(autoIdentifyHumans){
                UnityMainThreadDispatcher.Instance().Enqueue(() => DrawBoundingBoxes(message));
            }
            else{
                UnityMainThreadDispatcher.Instance().Enqueue(() => UpdateWithouDetection(message));
            }
        }
    }

    void UpdateWithouDetection(string detections)
    {
        UnityEngine.Vector3 pos = UnityEngine.Vector3.negativeInfinity;
        if(findHumanInView == true){

            string[] lines = detections.Split('\n');

            foreach (string line in lines)
            {
                // Debug.Log(line);
                if (string.IsNullOrWhiteSpace(line)) continue;
                
                string[] parts = line.Split(' ');  // Format: label confidence x1 y1 x2 y2
                // if (parts.Length < 6) continue;

                string label = parts[0];
                if(label == "person"){
                    // float confidence = float.Parse(parts[1]);
                    float x1 = float.Parse(parts[1]);
                    float y1 = float.Parse(parts[2]);
                    float x2 = float.Parse(parts[3]);
                    float y2 = float.Parse(parts[4]);
                    // Debug.Log(x1+" "+y1);

                    // Debug.Log($"🎯 Detected: {label} (Confidence: {confidence * 100:F2}%)");
                    Debug.Log($"🎯 Detected: {label}");

                    // Convert coordinates to Unity UI space (assuming normalized values)
                    float normalizedX = (x1 + x2) / 2;
                    float normalizedY = (y1 + y2) / 2;
                    UnityEngine.Vector3 position = new UnityEngine.Vector3(normalizedX, normalizedY, 0);
                    
                    
                    pos = GetHitPositionHumans(new UnityEngine.Vector2(normalizedX, normalizedY));
                    // Debug.Log("Positionn "+ GetHitPositionHumans(new UnityEngine.Vector2(normalizedX, normalizedY)));
                    

                    if(pos != UnityEngine.Vector3.negativeInfinity){
                        Debug.Log("!!!! Found Human in Scene!!!!" + pos);
                    }else{
                        Debug.Log("!!!! No Human in Scene, Try in Different Perspective!!!!");
                    }
                }
                
            }

            findHumanInView = false;

        }

        
    }

    void DrawBoundingBoxes(string detections)
    {
        // Clear previous bounding boxes
        foreach (GameObject box in activeBoundingBoxes)
        {
            
            box.SetActive(false);
            Destroy(box);
        }
        activeBoundingBoxes.Clear();

        string[] lines = detections.Split('\n');

        foreach (string line in lines)
        {
            Debug.Log(line);
            if (string.IsNullOrWhiteSpace(line)) continue;
            
            string[] parts = line.Split(' ');  // Format: label confidence x1 y1 x2 y2
            // if (parts.Length < 6) continue;

            string label = parts[0];
            if(label == "person"){
                // float confidence = float.Parse(parts[1]);
                float x1 = float.Parse(parts[1]);
                float y1 = float.Parse(parts[2]);
                float x2 = float.Parse(parts[3]);
                float y2 = float.Parse(parts[4]);
                Debug.Log(x1+" "+y1);

                // Debug.Log($"🎯 Detected: {label} (Confidence: {confidence * 100:F2}%)");
                Debug.Log($"🎯 Detected: {label}");

                // Convert coordinates to Unity UI space (assuming normalized values)
                float normalizedX = (x1 + x2) / 2;
                float normalizedY = (y1 + y2) / 2;
                UnityEngine.Vector3 position = new UnityEngine.Vector3(normalizedX, normalizedY, 0);
                
                GameObject bbox = Instantiate(boundingBoxPrefab, position, UnityEngine.Quaternion.identity, canvas.transform);
                bbox.transform.GetComponent<RectTransform>().sizeDelta = new UnityEngine.Vector3(x2 - x1, y2 - y1, 1);
                bbox.transform.GetComponent<RectTransform>().anchoredPosition = new UnityEngine.Vector2(normalizedX, -normalizedY);

                if(autoIdentifyHumans){
                    GetHitPositionHumans(new UnityEngine.Vector2(normalizedX, normalizedY));
                }

                
                activeBoundingBoxes.Add(bbox);
            }
            
        }
    }

    public UnityEngine.Vector3 GetHitPositionHumans(UnityEngine.Vector2 screenPosition){
        RaycastHit hit;
        var ray = Camera.main.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out hit))
        {
            Debug.Log("Collider " + hit.collider);
            if (hit.collider != null )
            {
                Debug.Log("Game Object " + hit.collider.transform.gameObject);
                if(hit.collider.transform.gameObject.tag == "Human"){
                    return(hit.collider.transform.position);
                }
                else{
                    return UnityEngine.Vector3.negativeInfinity;
                }
                
            }
            else{
                return UnityEngine.Vector3.negativeInfinity;
            }
        }
        else{
            return UnityEngine.Vector3.negativeInfinity;
        }
    }
}


// person 337.0726318359375 966.8889770507812 466.28192138671875 1058.448974609375


