using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using System;

public class gameManager2 : NetworkBehaviour
{
    public static gameManager2 Instance { get; private set; }

    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject buffPrefab;
    [SerializeField] private GameObject enemyPrefab;

    [Header("Spawning")]
    [SerializeField] private float buffSpawnInterval = 10f;
    [SerializeField] private float enemySpawnInterval = 6f;
    [SerializeField] private Vector2 spawnArea = new Vector2(15, 15);

    public Dictionary<string, PlayerData> playerStatesByAccountID = new();
    
    public event Action OnClientConnected;
    
    private float buffSpawnTimer;
    private float enemySpawnTimer;

    // --- Inicialización ---
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        }
        // Notifica a la UI que el cliente se ha conectado para mostrar el login.
        OnClientConnected?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }
    }

    // --- Lógica del Juego (Solo en el Servidor) ---
    void Update()
    {
        if (!IsServer) return;

        // Solo generar buffs y enemigos si hay al menos un jugador.
        if (NetworkManager.Singleton.ConnectedClients.Count > 0)
        {
            // Spawner de Buffs
            buffSpawnTimer += Time.deltaTime;
            if (buffSpawnTimer > buffSpawnInterval)
            {
                buffSpawnTimer = 0;
                Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-spawnArea.x, spawnArea.x), 0.5f, UnityEngine.Random.Range(-spawnArea.y, spawnArea.y));
                GameObject buff = Instantiate(buffPrefab, randomPos, Quaternion.identity);
                buff.GetComponent<NetworkObject>().Spawn(true);
            }

            // Spawner de Enemigos
            enemySpawnTimer += Time.deltaTime;
            if (enemySpawnTimer > enemySpawnInterval)
            {
                enemySpawnTimer = 0;
                Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-spawnArea.x, spawnArea.x), 0.5f, UnityEngine.Random.Range(-spawnArea.y, spawnArea.y));
                GameObject enemy = Instantiate(enemyPrefab, randomPos, Quaternion.identity);
                enemy.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }

    // --- Manejo de Jugadores ---

    private void HandleClientDisconnect(ulong clientId)
    {
        Debug.Log($"Cliente {clientId} se ha desconectado.");
        // El guardado de datos se hace en el OnNetworkDespawn del jugador.
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterPlayerServerRpc(string accountID, ulong clientID)
    {
        // Si es un jugador nuevo, crea sus datos.
        if (!playerStatesByAccountID.TryGetValue(accountID, out PlayerData data))
        {
            Debug.Log($"Registrando nuevo jugador: {accountID}");
            data = new PlayerData(accountID, GetRandomSpawnPosition(), 100, 5); // Stats iniciales
            playerStatesByAccountID[accountID] = data;
        }
        else
        {
            Debug.Log($"Bienvenido de nuevo: {accountID}");
        }
        
        // Instancia el jugador en el servidor.
        SpawnPlayerForClient(clientID, data);
    }

    private void SpawnPlayerForClient(ulong clientID, PlayerData data)
    {
        if (!IsServer) return;
        GameObject playerObject = Instantiate(playerPrefab);
        playerObject.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
        
        playerObject.GetComponent<SimplePlayerController>().SetData(data);
    }

    public Vector3 GetRandomSpawnPosition()
    {
        return new Vector3(UnityEngine.Random.Range(-spawnArea.x, spawnArea.x), 1f, UnityEngine.Random.Range(-spawnArea.y, spawnArea.y));
    }
}