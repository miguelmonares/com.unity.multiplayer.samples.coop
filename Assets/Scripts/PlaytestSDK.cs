using System;
using System.Collections;
using System.Collections.Generic;
using System.IO; 
using UnityEngine; 
using UnityEngine.Networking;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PlaytestAI.PlaytestSDK
{
    public class PlaytestSDK : MonoBehaviour 
{
    //------------------------------------------------------------------------------------------------
    // Connect to player controller.
    //------------------------------------------------------------------------------------------------
    [System.Serializable] public class SDKActionEvent : UnityEvent<string, JObject, Action> { }
    public SDKActionEvent onActionRequested;
    
    //------------------------------------------------------------------------------------------------
    // Action execution.
    //------------------------------------------------------------------------------------------------
    private Queue<Tuple<string, JObject, Action>> actionsQueue = new Queue<Tuple<string, JObject, Action>>();
    private bool isActionExecuting = false;

    ///-----------------------------------------------------------------------------------------------
    // Server communication.
    //------------------------------------------------------------------------------------------------
    private readonly string apiEndpoint =  "https://98xyua412c.execute-api.us-west-1.amazonaws.com";
    private string testCase = "";

    //------------------------------------------------------------------------------------------------
    // Called upon script enabled.
    //------------------------------------------------------------------------------------------------
    void Start() 
    {
        Initialize();
    }

    //------------------------------------------------------------------------------------------------
    // Initialize configs, game setup.
    //------------------------------------------------------------------------------------------------
    public virtual void Initialize()
    {
        testCase = EnvironmentVariables.GetEnvironmentVariable("TEST_CASE_STRING");
        testCase = "Walk in a square.";

        if (string.IsNullOrEmpty(testCase))
        {
            Debug.LogWarning("Test case string is empty or not set.");
            return;
        }
        Debug.Log($"Test case string loaded: {testCase}");

        StartCoroutine(MakePostRequest(testCase));
    }

    //------------------------------------------------------------------------------------------------
    // Format game state for server upload.
    //------------------------------------------------------------------------------------------------
    private string SerializeGameState()
    {
        // For testing, return a mock game state JSON
        string gameState = "{\"health\":100,\"position\":{\"x\":10,\"y\":20}," +
            "\"enemies\":5,\"inventory\":{\"ammo\":50},\"current_weapon\":\"rifle\"}";
            
        return gameState;
    }

    //------------------------------------------------------------------------------------------------
    // Initialize configs, game setup.
    //------------------------------------------------------------------------------------------------
    public IEnumerator ParseServerResponse(string jsonResponse)
    {
        var data = JObject.Parse(jsonResponse);

        foreach (var item in data["actions"])
        {
            string actionName = item["action"].Value<string>();
            var parameters = item["parameters"];

            EnqueueAction(actionName, (JObject) parameters, ActionComplete);
        }
        yield return null; 
    }

    //------------------------------------------------------------------------------------------------
    // Queue actions to be executed.
    //------------------------------------------------------------------------------------------------
    private void EnqueueAction(string actionName, JObject parameters, Action completionCallback)
    {
        actionsQueue.Enqueue(new Tuple<string, JObject, Action>(actionName, parameters, completionCallback));
        if (!isActionExecuting)
        {
            StartCoroutine(ExecuteActions());
        }
    }

    //------------------------------------------------------------------------------------------------
    // Execute actions queue.
    //------------------------------------------------------------------------------------------------
    private IEnumerator ExecuteActions()
    {

        while (actionsQueue.Count > 0 && !isActionExecuting)
        {
            isActionExecuting = true;

            Tuple<string, JObject, Action> nextAction = actionsQueue.Dequeue();
            string actionName = nextAction.Item1;
            JObject parameters = nextAction.Item2;
            Action completionCallback = nextAction.Item3;

            ExecuteAction(actionName, parameters, completionCallback);

            yield return new WaitUntil(() => !isActionExecuting);
        }

        isActionExecuting = false;
        AllActionsComplete();
    }


    
    //------------------------------------------------------------------------------------------------
    // Execute given action.
    //------------------------------------------------------------------------------------------------
    private void ExecuteAction(string actionName, JObject parameters, Action completionCallback)
    {
        Debug.Log($"Executing action: {actionName}");
        onActionRequested?.Invoke(actionName, parameters, ActionComplete);
    }

    //------------------------------------------------------------------------------------------------
    // Sample action stub for testing.
    //------------------------------------------------------------------------------------------------
    private IEnumerator DelayAction(float delayTime) {
        yield return new WaitForSeconds(delayTime);
        ActionComplete();
    }
    
    //------------------------------------------------------------------------------------------------
    // Signal completion of action.
    //------------------------------------------------------------------------------------------------
    public void ActionComplete() {
        isActionExecuting = false;
    }

    //------------------------------------------------------------------------------------------------
    // Called when all queued actions are complete.
    //------------------------------------------------------------------------------------------------
    private void AllActionsComplete() {
        Debug.Log("all actions complete.");
    }


    /*------------------------------------------------------------------------------------------------
    // SDK and game telemetry
    //------------------------------------------------------------------------------------------------
    public abstract void StartTelemetryLogging();
    public abstract void LogTelemetryData();
    public abstract void StopTelemetryLogging();
    public abstract void StartVideoRecording();
    public abstract void StopVideoRecording();
    ------------------------------------------------------------------------------------------------*/

    //------------------------------------------------------------------------------------------------
    // Close processes and clear resources gracefully.
    //------------------------------------------------------------------------------------------------
    public virtual void Shutdown()
    {
    }

    //------------------------------------------------------------------------------------------------
    // Send game state to agent and recieve list of directives.
    //------------------------------------------------------------------------------------------------
    public IEnumerator MakePostRequest(string testCase)
    {
        // Wait until the end of the frame to ensure all rendering is complete
        yield return new WaitForSeconds(3);
        yield return new WaitForEndOfFrame();

        // Capture screenshot.
        Texture2D screenCapture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenCapture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenCapture.Apply();
        byte[] imageBytes = screenCapture.EncodeToJPG();
        Destroy(screenCapture);

        // Convert image bytes to Base64 string
        string base64Image = Convert.ToBase64String(imageBytes);

        // Construct the payload with gameState, testCase, and screen capture.
        JObject payload = new JObject
        {
            // ["game_state"] = SerializeGameState(),
            ["task_description"] = new JObject { ["task"] = testCase },
            ["screen_capture"] = base64Image
        };
        string postData = payload.ToString(Formatting.None);

        // Make Post Request.
        UnityWebRequest request = UnityWebRequest.Put(apiEndpoint + "/update_game_state", postData);
        request.method = "POST";
        request.SetRequestHeader("Content-Type", "application/json");
        Debug.Log("Sending POST request with data: " + postData);

        // Send the request and wait for the response
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            // Request was successful, process the response
            Debug.Log("POST request successful! Response: " + request.downloadHandler.text.ToString());
            AgentResponse response = JsonUtility.FromJson<AgentResponse>
                (request.downloadHandler.text);

            if (response.actions.StartsWith("```json"))
            {
                string jsonExtracted = ExtractJsonFromMarkdown(response.actions);
                Debug.Log("Extracted Json response: " + jsonExtracted);
                StartCoroutine(ParseServerResponse(jsonExtracted)); 
            } else {
                Debug.Log(
                    "ERROR: Invalid server response formatting."
                );
            }
        }
        else
        {
            Debug.LogError("POST request failed! Error: " + request.error);
        }
    }

    //------------------------------------------------------------------------------------------------
    // Process server response into JSON.
    //------------------------------------------------------------------------------------------------
    private string ExtractJsonFromMarkdown(string markdownResponse)
    {
        string startDelimiter = "```json";
        string endDelimiter = "```";

        int startIndex = markdownResponse.IndexOf(startDelimiter) + startDelimiter.Length;
        int endIndex = markdownResponse.IndexOf(endDelimiter, startIndex);

        if (startIndex < 0 || endIndex < 0)
        {
            return "";
        }

        string jsonContent = markdownResponse.Substring(startIndex, endIndex - startIndex).Trim();
        return jsonContent;
    }

}

//----------------------------------------------------------------------------------------------------
// Helper classes.
//----------------------------------------------------------------------------------------------------
public class UpdateGameStateResponse
{
    public string job_id;
}

public class CheckStatusResponse
{
    public string status;
    public string result;
}

public class AgentResponse
{
    public string actions;
}
}
