using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class PlaytestServer : MonoBehaviour
{
    // Configuration for connecting to the Python server
    public string serverIP = "localhost";
    public int serverPort = 25250;

    // Pause/Unpause actions need to be called on the main thread.
    private readonly Queue<Action> mainThreadActions = new();

    private TcpClient client;
    private Thread clientThread;
    private Canvas overlayCanvas;

    private GameObject overlayPanel;
    private NetworkStream stream;

    private void Awake()
    {
        // Make this GameObject persistent
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CreatePauseScreen();
        clientThread = new Thread(ConnectToServer);
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    private void Update()
    {
        // Execute all queued actions on the main thread
        while (mainThreadActions.Count > 0)
        {
            Action action = null;
            lock (mainThreadActions)
            {
                if (mainThreadActions.Count > 0) action = mainThreadActions.Dequeue();
            }

            action?.Invoke();
        }
    }

    private void OnDestroy()
    {
        // Cleanup resources when the object is destroyed
        stream?.Close();
        client?.Close();
        clientThread?.Abort();
    }

    private void CreatePauseScreen()
    {
        var canvasObject = new GameObject("OverlayCanvas");
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 1000; // High sorting order to ensure it is on top

        // Create Panel under the new Canvas
        overlayPanel = new GameObject("OverlayPanel");
        overlayPanel.transform.SetParent(overlayCanvas.transform, false); // Set the new canvas as parent

        // Add a CanvasRenderer
        overlayPanel.AddComponent<CanvasRenderer>();

        // Add an Image Component and set it to black
        var panelImage = overlayPanel.AddComponent<Image>();
        panelImage.color = Color.black;

        // Make the panel full-screen
        var rect = overlayPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(0, 0);
        rect.offsetMax = new Vector2(0, 0);

        // Initially hide the panel
        overlayPanel.SetActive(false);

        DontDestroyOnLoad(overlayCanvas);
    }

    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient(serverIP, serverPort);
            stream = client.GetStream();
            Debug.Log("Connected to Python server.");

            ListenForCommands();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to connect to server: {e.Message}");
        }
    }

    private void ListenForCommands()
    {
        var bytes = new byte[1024];
        try
        {
            while (true)
            {
                var length = stream.Read(bytes, 0, bytes.Length);
                if (length != 0)
                {
                    var command = Encoding.UTF8.GetString(bytes, 0, length);
                    Debug.Log($"Received command: {command}");

                    if (command.Equals("pause", StringComparison.OrdinalIgnoreCase))
                        EnqueueMainThreadAction(Pause);
                    else if (command.Equals("unpause", StringComparison.OrdinalIgnoreCase))
                        EnqueueMainThreadAction(Unpause);
                    else if (command.Equals("quit", StringComparison.OrdinalIgnoreCase))
                        EnqueueMainThreadAction(QuitGame);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in ListenForCommands: {e.Message}");
        }
        finally
        {
            stream?.Close();
            client?.Close();
        }
    }

    private void Pause()
    {
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(true);
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
        else
        {
            Debug.LogWarning("overlayPanel is null.");
        }
    }

    private void Unpause()
    {
        if (overlayPanel != null)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            overlayPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("overlayPanel is null.");
        }
    }

    private void QuitGame()
    {
        Application.Quit();
        // OnDestroy should handle clean up here.
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }
}
