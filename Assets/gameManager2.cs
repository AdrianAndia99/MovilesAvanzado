using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class gameManager2 : NetworkBehaviour
{
    public static gameManager2 Instance;
    public Dictionary<string, PlayerData> playerStatesByAccountID = new();

    public GameObject playerPrefab;
    public Action OnConnection;
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
            OnConnection?.Invoke();
    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }
    }
    private void HandleDisconnect(ulong clientID)
    {
        print("jugador " + clientID + " se fue");

    }

    [Rpc(SendTo.Server)]
    public void RegisterPlayerServerRpc(string accountID, ulong ID)
    {
        if (!playerStatesByAccountID.TryGetValue(accountID, out PlayerData data))
        {
            PlayerData newData = new PlayerData(accountID, Vector3.zero, 100, 5);
            playerStatesByAccountID[accountID] = newData;
            // instanciar player con data
            SpawnPlayerServer(ID, newData);
            print("nuevo jugador " + accountID);
        }
        else
        {
            print("bienvenido de nuevo " + accountID);
            SpawnPlayerServer(ID, data);

        }
    }
    public void SpawnPlayerServer(ulong ID, PlayerData data)
    {
        if (!IsServer) return;
        GameObject player = Instantiate(playerPrefab);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(ID, true);
        player.GetComponent<player2>().SetData(data);        
    }
}
