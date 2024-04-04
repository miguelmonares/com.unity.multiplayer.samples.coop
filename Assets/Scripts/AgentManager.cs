using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using AYellowpaper.SerializedCollections;

public class AgentManager : MonoBehaviour
{
    [SerializedDictionary("Element Type", "Description")]
    public SerializedDictionary<string, string> ElementDescriptions2;
    
    //  public PlayerController playerController;
    public PlaytestAgent agent;
    
    private void Awake()
    {
        /* Assign controllers on wake.
        if (!playerController)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        */
        if (!agent)
        {
            agent = FindObjectOfType<PlaytestAgent>();
        }
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
            case "MoveInDirection":
                string direction = parameters["direction"].Value<string>();
                Debug.Log("Performing MoveInDirection in the direction of " + direction);
                agent.MoveInDirection(direction, onComplete);
                break;
            case "MoveTowardNearestEnemy":
                Debug.Log("Performing MoveTowardNearestEnemy");
                agent.MoveTowardNearestEnemy(onComplete);
                break;
            case "AttackInDirection":
                string attackDirection = parameters["direction"].Value<string>();
                Debug.Log("Performing AttackInDirection in the direction of " + attackDirection);
                agent.AttackInDirection(attackDirection, onComplete);
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
