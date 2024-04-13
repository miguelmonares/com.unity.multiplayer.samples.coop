using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using System.Collections.Concurrent;


public class CommandClient : MonoBehaviour
{
    // Configuration for connecting to the Python server
    public string serverIP = "localhost";
    public int serverPort = 8052;

    private TcpClient client;
    private NetworkStream stream;
    private Thread clientThread;

    private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    void Start()
    {
        clientThread = new Thread(ConnectToServer);
        clientThread.IsBackground = true;
        clientThread.Start();
    }

    void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
        {
            action.Invoke();
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
                        Time.timeScale = 0f; // pause
                    }
                    else if (command.Equals("unpause", StringComparison.OrdinalIgnoreCase))
                    {
                        Time.timeScale = 1f; // unpause
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
}
