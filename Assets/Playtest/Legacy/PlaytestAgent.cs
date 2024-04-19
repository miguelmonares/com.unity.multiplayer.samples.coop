using System.Collections;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.Netcode;
using UnityEngine;

public class PlaytestAgent : MonoBehaviour
{
    // References to different scripts in the game.
    private ServerCharacter serverCharacter;

    //------------------------------------------------------------------------------------------------
    // SETUP
    //------------------------------------------------------------------------------------------------
    // This gets called whenever the object/script is activated. This object activates when the game scene starts.
    private void OnEnable()
    {
        ClientPlayerAvatar.LocalClientSpawned += OnLocalClientSpawned;
        Debug.Log("Agent initialized.");
    }

    // This object deactivates when the game scene ends.
    private void OnDisable()
    {
        ClientPlayerAvatar.LocalClientSpawned -= OnLocalClientSpawned;
    }

    // This gets called when the player logs in.
    private void OnLocalClientSpawned(ClientPlayerAvatar avatar)
    {
        // Get the ServerCharacter component from the avatar
        serverCharacter = avatar.GetComponent<ServerCharacter>();
    }

    //------------------------------------------------------------------------------------------------
    // EXPOSED AI ACTIONS
    //------------------------------------------------------------------------------------------------
    public void MoveToNearestObject(string type)
    {
        var obj = GetNearestObjectOfType(type);
        // If object is found, move towards it
        if (obj != null)
        {
            var destination =
                ActionUtils.GetDashDestination(serverCharacter.transform, obj.transform.position, true);
            serverCharacter.SendCharacterInputServerRpc(destination);
            StartCoroutine(MoveToTransform(obj.transform));
        }
    }

    public void AttackNearestObject(string type)
    {
        var obj = GetNearestObjectOfType(type);
        if (obj != null)
        {
            ulong[] objIds = null;
            if (obj.GetComponent<NetworkObject>())
                objIds = new[] { obj.GetComponent<NetworkObject>().NetworkObjectId };
            var action = new ActionRequestData
            {
                ActionID = serverCharacter.CharacterClass.Skill1.ActionID,
                ShouldQueue = false,
                ShouldClose = true,
                TargetIds = objIds
            };
            serverCharacter.RecvDoActionServerRPC(action);
        }
    }

    public void StressTestCurrentPos()
    {
        IEnumerator Helper(float radius, float duration)
        {
            var initialPosition = serverCharacter.transform.position;
            var timer = 0f;
            while (timer < duration)
            {
                var randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0f;
                randomDirection.Normalize();

                var targetPosition = initialPosition + randomDirection * radius;
                serverCharacter.SendCharacterInputServerRpc(targetPosition);
                while (serverCharacter.MovementStatus.Value != MovementStatus.Idle)
                {
                    timer += Time.deltaTime;
                    yield return null;
                }

                yield return null;
            }
        }

        StartCoroutine(Helper(2, 30));
    }


    //------------------------------------------------------------------------------------------------
    // HELPER FUNCTIONS
    //------------------------------------------------------------------------------------------------
    private GameObject GetNearestObjectOfType(string type)
    {
        var detectionRadius = 100f; // Set the detection radius
        // Create a layer mask for the NPCs layer
        var layerMask = LayerMask.GetMask(type);

        // Use the layer mask in the OverlapSphere call to only get colliders on the NPCs layer
        var hitColliders = Physics.OverlapSphere(serverCharacter.transform.position, detectionRadius, layerMask);
        Collider closestObject = null;
        var closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            var distance = Vector3.Distance(hitCollider.transform.position, serverCharacter.transform.position);
            if (distance < closestDistance && distance > 1f)
            {
                closestDistance = distance;
                closestObject = hitCollider;
            }
        }

        return closestObject?.gameObject;
    }

    private IEnumerator MoveToTransform(Transform transform)
    {
        var completionRadius = 1f; // The radius within which the action is considered complete
        while (true)
        {
            var distance = Vector3.Distance(serverCharacter.transform.position, transform.position);
            if (distance <= completionRadius)
                // Agent is at destination
                yield break;
            serverCharacter.SendCharacterInputServerRpc(transform.position);
            yield return new WaitForSeconds(1f);
        }
    }
}
