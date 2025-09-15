using Unity.Netcode;
using UnityEngine;

public class RandomBuff : NetworkBehaviour
{
    [SerializeField] private int buffAmount = 1;
    // Prevent multiple applications while despawn is processing
    private bool applied = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") && !applied)
        {
            // Try to get the NetworkObject of the player that collided
            var no = other.GetComponent<NetworkObject>();
            if (no != null)
            {
                // Choose random buff between 1 and 3 (inclusive)
                int chosen = Random.Range(1, 4); // upper bound is exclusive
                // Send server rpc with the owner client id of the colliding player and the chosen amount
                AddBuffPlayerServerRpc(no.OwnerClientId, chosen);
                applied = true;
                print("Hemos chocao con owner " + no.OwnerClientId + " amount " + chosen);
            }
        }
    }

    // ServerRpc: apply the buff to the specific player and despawn the buff object
    [ServerRpc(RequireOwnership = false)]
    private void AddBuffPlayerServerRpc(ulong playerID, int amount)
    {
        if (!IsServer) return;
        print("aplicar buf en servidor a " + playerID + " amount " + amount);

        // Find the player's NetworkObject via connected clients
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(playerID, out var client))
        {
            var playerObject = client.PlayerObject;
            if (playerObject != null)
            {
                var controller = playerObject.GetComponent<SimplePlayerController>();
                if (controller != null)
                {
                    // Increase only this player's attack NetworkVariable by the chosen amount
                    controller.attack.Value += amount;
                    print($"Nuevo attack de {playerID} = {controller.attack.Value}");
                }
            }
        }

        // Despawn the buff object on the server so all clients see it disappear
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null)
        {
            netObj.Despawn(true);
        }
    }
}