using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Concurrent;
using System.Collections.Generic;


public class PlaytestServer : MonoBehaviour
{
    // Configuration for connecting to the Python server
    public string serverIP = "localhost";
    public int serverPort = 25250;

    private TcpClient client;
    private NetworkStream stream;
    private Thread clientThread;

    // Pause/Unpause actions need to be called on the main thread.
    private Queue<Action> mainThreadActions = new Queue<Action>();

    private GameObject overlayPanel;
    private Canvas overlayCanvas;

    void Awake()
    {
        // Make this GameObject persistent
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        GameObject canvasObject = new GameObject("OverlayCanvas");
        overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 1000;  // High sorting order to ensure it is on top

        // Create Panel under the new Canvas
        overlayPanel = new GameObject("OverlayPanel");
        overlayPanel.transform.SetParent(overlayCanvas.transform, false);  // Set the new canvas as parent

        // Add a CanvasRenderer
        overlayPanel.AddComponent<CanvasRenderer>();

        // Add an Image Component and set it to black
        Image panelImage = overlayPanel.AddComponent<Image>();
        panelImage.color = Color.black;

        // Make the panel full-screen
        RectTransform rect = overlayPanel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 1);
        rect.offsetMin = new Vector2(0, 0);
        rect.offsetMax = new Vector2(0, 0);

        // Initially hide the panel
        overlayPanel.SetActive(false);

        DontDestroyOnLoad(overlayCanvas);

        clientThread = new Thread(ConnectToServer);
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    void Update()
    {
        // Execute all queued actions on the main thread
        while (mainThreadActions.Count > 0)
        {
            Action action = null;
            lock (mainThreadActions)
            {
                if (mainThreadActions.Count > 0)
                {
                    action = mainThreadActions.Dequeue();
                }
            }

            action?.Invoke();
        }
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
        byte[] bytes = new byte[1024];
        try
        {
            while (true)
            {
                int length = stream.Read(bytes, 0, bytes.Length);
                if (length != 0)
                {
                    var command = Encoding.UTF8.GetString(bytes, 0, length);
                    Debug.Log($"Received command: {command}");

                    if (command.Equals("pause", StringComparison.OrdinalIgnoreCase))
                    {
                        
                        EnqueueMainThreadAction(() =>
                        {
                            if (overlayPanel != null)
                            {
                                Time.timeScale = 0f; // pause
                                overlayPanel.SetActive(true);
                            }
                            else
                            {
                                Debug.LogWarning("overlayPanel is null.");
                        }
                        });

                    }
                    else if (command.Equals("unpause", StringComparison.OrdinalIgnoreCase))
                    {
                        EnqueueMainThreadAction(() =>
                        {
                            if (overlayPanel != null)
                            {
                                overlayPanel.SetActive(false);
                                Time.timeScale = 1f; // unpause
                            }
                            else
                            {
                                Debug.LogWarning("overlayPanel is null.");
                            }
                        });
                        
                    }
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

    void OnDestroy()
    {
        // Cleanup resources when the object is destroyed
        stream?.Close();
        client?.Close();
        clientThread?.Abort();
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

}
