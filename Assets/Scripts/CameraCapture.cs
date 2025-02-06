using UnityEngine;
using Unity.Barracuda;
using System.Collections;
using System.Linq;

public class CameraCapture : MonoBehaviour
{
    public NNModel modelAsset;  // Assign your YOLOv11 ONNX model in Unity Inspector
    private IWorker worker;
    private Camera playerCamera;
    public Texture2D captured;

    private const int INPUT_WIDTH = 640;
    private const int INPUT_HEIGHT = 640;

    void Start()
    {
        playerCamera = GetComponent<Camera>();

        // Load the model into Barracuda
        var model = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
    }

    void Update()
    {
        StartCoroutine(ProcessFrame());
    }

    IEnumerator ProcessFrame()
    {
        yield return new WaitForEndOfFrame(); // Wait until the frame is fully rendered

        // Capture current frame
        Texture2D screenshot = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        RenderTexture renderTexture = new RenderTexture(Screen.width, Screen.height, 24);
        playerCamera.targetTexture = renderTexture;
        playerCamera.Render();

        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenshot.Apply();

        playerCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(renderTexture);

        // Resize image to match model input size
        Texture2D resizedTexture = ResizeTexture(screenshot, INPUT_WIDTH, INPUT_HEIGHT);
        captured = resizedTexture;

        // Process frame with YOLO model
        DetectHumans(resizedTexture);
    }

    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = new RenderTexture(newWidth, newHeight, 24);
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        RenderTexture.active = rt;
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        return result;
    }

    void DetectHumans(Texture2D image)
    {
        Tensor inputTensor = TransformInput(image);
        worker.Execute(inputTensor);
        Tensor outputTensor = worker.PeekOutput("output0"); // Replace with the actual output name of the YOLO model
        ProcessDetections(outputTensor);
        inputTensor.Dispose();
        outputTensor.Dispose();
    }

    Tensor TransformInput(Texture2D image)
    {
        // Convert Texture2D to Tensor
        float[] floatValues = new float[image.width * image.height * 3];
        Color[] pixels = image.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            floatValues[i * 3 + 0] = pixels[i].r;
            floatValues[i * 3 + 1] = pixels[i].g;
            floatValues[i * 3 + 2] = pixels[i].b;
        }

        return new Tensor(1, image.height, image.width, 3, floatValues);
    }

    void ProcessDetections(Tensor outputTensor)
    {
        int numDetections = outputTensor.shape[1]; // Number of detected objects
        int attributeCount = outputTensor.shape[2]; // Number of attributes per detection (should be 6)

        for (int i = 0; i < numDetections; i++)
        {
            float confidence = outputTensor[i, 4]; // Confidence score
            int classId = Mathf.RoundToInt(outputTensor[i, 5]); // Class ID
            Debug.Log(confidence + ", " +  classId);

            if (confidence > 0.5f && classId == 0) // Only detect humans (Class ID 0 in COCO)
            {
                float x = outputTensor[i, 0]; // X Center
                float y = outputTensor[i, 1]; // Y Center
                float width = outputTensor[i, 2];
                float height = outputTensor[i, 3];

                DrawBoundingBox(x, y, width, height);
            }
        }
    }

    void DrawBoundingBox(float x, float y, float width, float height)
    {
        // Convert YOLO coordinates to screen space
        Vector2 screenPosition = new Vector2(x * Screen.width, (1 - y) * Screen.height);
        Vector2 boxSize = new Vector2(width * Screen.width, height * Screen.height);

        // Create a UI bounding box (You can use Unity UI for better visualization)
        GameObject boundingBox = new GameObject("BoundingBox");
        RectTransform rectTransform = boundingBox.AddComponent<RectTransform>();
        rectTransform.sizeDelta = boxSize;
        rectTransform.position = screenPosition;

        // Assign UI Image or line renderer for visibility
        boundingBox.AddComponent<UnityEngine.UI.Image>().color = Color.red;
    }

    void OnDestroy()
    {
        worker.Dispose();
    }
}
