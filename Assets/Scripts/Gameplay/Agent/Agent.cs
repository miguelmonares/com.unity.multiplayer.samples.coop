using UnityEngine;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.UserInput;
using System.Linq;

public class Agent : MonoBehaviour
{
    // References to different scripts in the game.
    private ServerCharacterMovement characterMovement;
    private ServerCharacter serverCharacter; 
    private ClientInputSender inputSender; 

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
            else if (Input.GetKeyDown(KeyCode.I))
            {
                AttackInDirection("forward");
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                AttackInDirection("backward");
            }
            else if (Input.GetKeyDown(KeyCode.J))
            {
                AttackInDirection("left");
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                AttackInDirection("right");
            }
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            AttackInDirection("forward");
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

    public void AttackInDirection(string direction) {
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

    public void AttackInDirection(string direction)
    {
        // Assuming direction is "forward", "backward", "left", or "right"
        // You might need to adjust this method to actually determine the direction vector based on the input
        // For simplicity, let's just use the "forward" direction as an example

        if (direction.ToLower() == "forward")
        {
            // Trigger the first ability (Skill1) as an example
            // You need to replace `actionState1.actionID` with the actual ActionID for the ability you want to trigger
            if (inputSender != null && serverCharacter.CharacterClass.Skill1 != null)
            {
                inputSender.RequestAction(serverCharacter.CharacterClass.Skill1.ActionID, ClientInputSender.SkillTriggerStyle.Keyboard);
            }
        }
        // Add similar conditions for "backward", "left", "right" to trigger different abilities or the same ability in different directions
    }
}