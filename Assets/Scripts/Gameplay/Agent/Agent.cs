using UnityEngine;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.UserInput;
using System.Collections;
using System.Linq;
using System;

namespace Unity.BossRoom.Gameplay.Agent
{
public class Agent : MonoBehaviour
{
    // References to different scripts in the game.
    private ServerCharacterMovement characterMovement;
    private ServerCharacter serverCharacter; 
    private ClientInputSender inputSender; 

    // Define a delegate for action completion
    public delegate void ActionCompletedDelegate(string message);
    public event ActionCompletedDelegate OnActionCompleted;

    // This gets called whenever the object/script is activated. This object activates when the game scene starts.
    void OnEnable()
    {
        ClientPlayerAvatar.LocalClientSpawned += OnLocalClientSpawned;
        Debug.Log("Agent initialized.");
    }

    // This object deactivates when the game scene ends.
    void OnDisable()
    {
        ClientPlayerAvatar.LocalClientSpawned -= OnLocalClientSpawned;
    }

    // This gets called once per frame.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // MoveTowardNearestEnemy(null);
            ListAllObjects();
        }
        
        if(serverCharacter != null) // Ensure serverCharacter is not null
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                MoveInDirection("forward", null);
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                MoveInDirection("backward", null);
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                MoveInDirection("left", null);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                MoveInDirection("right", null);
            }
            else if (Input.GetKeyDown(KeyCode.I))
            {
                AttackInDirection("forward", null);
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                AttackInDirection("backward", null);
            }
            else if (Input.GetKeyDown(KeyCode.J))
            {
                AttackInDirection("left", null);
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                AttackInDirection("right", null);
            }
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            AttackInDirection("forward", null);
        }
    }

    // This gets called when the player logs in.
    private void OnLocalClientSpawned(ClientPlayerAvatar avatar)
    {
        // Get the ServerCharacterMovement and ServerCharacter components from the avatar
        characterMovement = avatar.GetComponent<ServerCharacterMovement>();
        serverCharacter = avatar.GetComponent<ServerCharacter>(); // Obtain the ServerCharacter component
        inputSender = avatar.GetComponent<ClientInputSender>(); // Obtain the ClientInputSender component


        Debug.Log("Testing agent movement.");
    }

    public void MoveInDirection(string direction, System.Action onComplete)
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
        StartCoroutine(ActionCompleteAfterDelay(onComplete, 1f));
    }

    public void AttackInDirection(string direction, System.Action onComplete) {
        Vector3 playerPosition = serverCharacter.transform.position;
        Vector3 moveDirection = Vector3.zero;
        Vector3 targetDirection = Vector3.zero;
        float moveDistance = 2f; // Distance to move in the specified direction
        switch (direction.ToLower())
        {
            case "forward":
                moveDirection = transform.forward * moveDistance;
                targetDirection = Vector3.forward;
                break;
            case "backward":
                moveDirection = -transform.forward * moveDistance;
                targetDirection = Vector3.back;
                break;
            case "left":
                moveDirection = -transform.right * moveDistance;
                targetDirection = Vector3.left;
                break;
            case "right":
                moveDirection = transform.right * moveDistance;
                targetDirection = Vector3.right;
                break;
        }

        Vector3 targetPosition = playerPosition + moveDirection;

        var action = new ActionRequestData {
            Position = targetPosition,
            Direction = targetDirection,
            ActionID = serverCharacter.CharacterClass.Skill1.ActionID,
            ShouldQueue = false,
            TargetIds = null
        };

        serverCharacter.RecvDoActionServerRPC(action);
        StartCoroutine(ActionCompleteAfterDelay(onComplete, .5f));
    }

    public void MoveTowardNearestEnemy(System.Action onComplete)
    {
        Debug.Log("invoked MoveTowardNearestEnemy");
        float detectionRadius = 20f; // Set the detection radius
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
            StartCoroutine(CheckDistanceAndComplete(closestEnemy.transform, onComplete));
        }
        else
        {
            Debug.Log("No enemies detected within radius.");
            OnActionCompleted?.Invoke("Move Toward Nearest Enemy Completed");
            onComplete?.Invoke(); // Invoke completion if no enemy is found
        }
    }

    private IEnumerator ActionCompleteAfterDelay(System.Action onComplete, float delay)
    {
        yield return new WaitForSeconds(delay);
        onComplete?.Invoke();
    }

    private IEnumerator CheckDistanceAndComplete(Transform enemyTransform, System.Action onComplete)
    {
        float completionRadius = 1f; // The radius within which the action is considered complete
        while (true)
        {
            float distanceToEnemy = Vector3.Distance(serverCharacter.transform.position, enemyTransform.position);
            if (distanceToEnemy <= completionRadius)
            {
                Debug.Log("Agent is within completion radius of the enemy.");
                OnActionCompleted?.Invoke("Move Toward Nearest Enemy Completed");

                onComplete?.Invoke();
                yield break; // Exit the coroutine
            }
            yield return new WaitForSeconds(0.1f); // Check distance every 0.1 seconds
        }
    }

    private void ListAllObjects()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>() ;
        foreach(GameObject go in allObjects)
        {
        if (go.activeInHierarchy)
        {
            print(go.name+" is an active object") ;
        }
        }
    }
}
}