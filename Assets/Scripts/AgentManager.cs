using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;

public class AgentManager : MonoBehaviour
{
    //  public PlayerController playerController;


    private void Awake()
    {
        /* Assign controllers on wake.
        if (!playerController)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        */
    }

    public void PerformAction(string actionName, JObject parameters, Action onComplete)
    {
        switch (actionName)
        {
            // Register action stubs here.
            case "Shoot":
                Debug.Log("Shooting the gun");
                Shoot();
                onComplete?.Invoke();
                break;
            default:
                Debug.LogError("Action not recognized: " + actionName);
                break;
            
        }
    }

    public void Shoot() {
        Debug.Log("Simulating Shoot");
    }
    
}
