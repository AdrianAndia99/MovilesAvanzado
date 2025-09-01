using UnityEngine;
using Unity.Netcode;
public class GameManager : NetworkBehaviour
{
    private static GameManager Instance;


    [SerializeField] private Transform player;
    [SerializeField] private GameObject buffP;
    [SerializeField] private GameObject enemyPrefab;

    public float spawnCount = 4f;
    public float currentCount = 0;
    public float enemySpawnInterval = 6f;

    private float enemySpawnTimer = 0f;

    void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        
    }
    private void Update()
    {
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            currentCount += Time.deltaTime;
            if (currentCount > spawnCount)
            {
                Vector3 random = new Vector3(Random.Range(-8, 8), 0.5f, (Random.Range(-8, 8)));
                GameObject buff = Instantiate(buffP);
                buff.GetComponent<NetworkObject>().Spawn(true);
                currentCount = 0;
            }
        }

        // Spawner de enemigos
        if (IsServer && NetworkManager.Singleton.ConnectedClients.Count >= 2)
        {
            enemySpawnTimer += Time.deltaTime;
            if (enemySpawnTimer > enemySpawnInterval)
            {
                Vector3 random = new Vector3(Random.Range(-8, 8), 0.5f, Random.Range(-8, 8));
                GameObject enemy = Instantiate(enemyPrefab, random, Quaternion.identity);
                enemy.GetComponent<NetworkObject>().Spawn(true);
                enemySpawnTimer = 0;
            }
        }
    }
    public override void OnNetworkSpawn()
    {
        print("Current pLAYER" + NetworkManager.Singleton.ConnectedClients.Count);
        print(NetworkManager.Singleton.LocalClientId);
        InstancePlayerRpc(NetworkManager.Singleton.LocalClientId);
    }

    [Rpc(SendTo.Server)]
    public void InstancePlayerRpc(ulong ownerID)
    {
        Transform playerP = Instantiate(player);
        playerP.GetComponent<SimplePlayerController>().PlayerID.Value = ownerID;
        playerP.GetComponent<NetworkObject>().SpawnWithOwnership(ownerID, true);
    }

    public static GameManager instance_ => Instance;
}
