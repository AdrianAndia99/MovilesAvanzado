using Unity.Netcode;
using UnityEngine;

public class Projectil : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (IsServer)
        {
            Invoke("SimpleDespawn", 5f);
        }
    }


    public void SimpleDespawn()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }

    public void OnCollisionEnter(Collision collision)
    {
        
    }
}
