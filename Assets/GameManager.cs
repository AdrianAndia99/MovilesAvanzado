using UnityEngine;
using Unity.Netcode;
public class GameManager : NetworkBehaviour
{
    private static GameManager Instance;


    [SerializeField] private Transform player;

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

    public override void OnNetworkSpawn()
    {
        print(NetworkManager.Singleton.LocalClientId);
        InstancePlayerRpc(NetworkManager.Singleton.LocalClientId);
    }
    [Rpc(SendTo.Server)]
    public void InstancePlayerRpc(ulong ownerID)
    {
        Transform playerP = Instantiate(player);
        playerP.GetComponent<NetworkObject>().SpawnWithOwnership(ownerID, true);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

    public static GameManager instance_ => Instance;
}
