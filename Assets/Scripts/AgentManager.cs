using UnityEngine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;

public class AgentManager : MonoBehaviour
{
    //  public PlayerController playerController;
    public AgentManager agent;


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
            case "MoveInDirection":
                Debug.Log("Performing MoveInDirection");
                float direction = parameters["direction"].Value<float>();
                agent.MoveInDirection(direction);
                break;
            case "MoveTowardNearestEnemy":
                Debug.Log("Performing MoveTowardNearestEnemy");
                agent.MoveTowardNearestEnemy();
                break;
            case "AttackInDirection":
                Debug.Log("Performing AttackInDirection");
                string attackDirection = parameters["direction"].Value<string>();
                agent.AttackInDirection(attackDirection);
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
