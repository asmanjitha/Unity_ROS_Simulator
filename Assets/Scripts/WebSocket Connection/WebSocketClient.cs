using UnityEngine;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Text;
using System.Collections;
using System.Threading;

public class WebSocketClient : MonoBehaviour
{
    private ClientWebSocket ws;
    private bool isConnected = false;

    async void Start()  //  Make Start() async
    {
        ws = new ClientWebSocket();
        await ws.ConnectAsync(new System.Uri("ws://localhost:4200"), CancellationToken.None);
        isConnected = true;
        Debug.Log("Connected to YOLO server");

        _ = Task.Run(ReceiveMessages);  //  Run ReceiveMessages() in a background task
    }

    public async void SendFrame(byte[] imageBytes)  //  Make this method async
    {
        if (isConnected)
        {
            await ws.SendAsync(new System.ArraySegment<byte>(imageBytes), WebSocketMessageType.Binary, true, CancellationToken.None);
        }
    }

    private async Task ReceiveMessages()  //  Make this an async Task
    {
        byte[] buffer = new byte[1024];

        while (true)
        {
            var result = await ws.ReceiveAsync(new System.ArraySegment<byte>(buffer), CancellationToken.None);
            string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            Debug.Log("Received detections: " + message);
        }
    }
}
