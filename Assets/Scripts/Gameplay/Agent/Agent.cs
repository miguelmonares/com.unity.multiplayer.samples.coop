using UnityEngine;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Actions;
using System.Linq;

public class Agent : MonoBehaviour
{
    private ServerCharacterMovement characterMovement;
    private ServerCharacter serverCharacter; // Reference to the ServerCharacter component

    void OnEnable()
    {
        ClientPlayerAvatar.LocalClientSpawned += OnLocalClientSpawned;
        Debug.Log("Agent initialized.");
    }

    void OnDisable()
    {
        ClientPlayerAvatar.LocalClientSpawned -= OnLocalClientSpawned;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            MoveTowardNearestEnemy();
        }
        
        if(serverCharacter != null) // Ensure serverCharacter is not null
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                MoveInDirection("forward");
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                MoveInDirection("backward");
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                MoveInDirection("left");
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                MoveInDirection("right");
            }
        }
    }

    private void OnLocalClientSpawned(ClientPlayerAvatar avatar)
    {
        // Get the ServerCharacterMovement and ServerCharacter components from the avatar
        characterMovement = avatar.GetComponent<ServerCharacterMovement>();
        serverCharacter = avatar.GetComponent<ServerCharacter>(); // Obtain the ServerCharacter component

        Debug.Log("Testing agent movement.");
    }

    public void MoveInDirection(string direction)
    {
        Vector3 playerPosition = serverCharacter.transform.position;
        Vector3 moveDirection = Vector3.zero;
        float moveDistance = 2f; // Distance to move in the specified direction
        switch (direction.ToLower())
        {
            case "forward":
                moveDirection = transform.forward * moveDistance;
                break;
            case "backward":
                moveDirection = -transform.forward * moveDistance;
                break;
            case "left":
                moveDirection = -transform.right * moveDistance;
                break;
            case "right":
                moveDirection = transform.right * moveDistance;
                break;
        }
        Debug.Log($"Moving the agent in direction: {direction}");

        // Use SendCharacterInputServerRpc for movement
        Vector3 targetPosition = playerPosition + moveDirection;
        serverCharacter.SendCharacterInputServerRpc(targetPosition);
    }

    public void MoveTowardNearestEnemy()
    {
        Debug.Log("invoked MoveTowardNearestEnemy");
        float detectionRadius = 10f; // Set the detection radius
        // Create a layer mask for the NPCs layer
        int layerMask = LayerMask.GetMask("NPCs");
        
        // Use the layer mask in the OverlapSphere call to only get colliders on the NPCs layer
        Collider[] hitColliders = Physics.OverlapSphere(serverCharacter.transform.position, detectionRadius, layerMask);
        Collider closestEnemy = null;
        float closestDistanceSqr = Mathf.Infinity;
        Vector3 currentPosition = serverCharacter.transform.position;

        // Iterate through all found colliders to find the closest one
        foreach (var hitCollider in hitColliders)
        {
            Debug.Log($"Enemy found: {hitCollider.gameObject.name}, Distance: {Vector3.Distance(currentPosition, hitCollider.transform.position)}");
            Vector3 directionToTarget = hitCollider.transform.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                closestEnemy = hitCollider;
            }
        }

        // If an enemy is found, move towards it
        if (closestEnemy != null)
        {
            Vector3 destination = ActionUtils.GetDashDestination(serverCharacter.transform, closestEnemy.transform.position, true);
            serverCharacter.SendCharacterInputServerRpc(destination);
        }
        else
        {
            Debug.Log("No enemies detected within radius.");
        }
    }
}